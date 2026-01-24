using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http
{
    /// <summary>
    /// HTTP interceptor that returns mock data for IGDB and Steam API calls.
    /// Enable by setting the GAMARR_MOCK_METADATA environment variable to "true".
    /// Mock data files should be placed in the test Files/MockData directory.
    /// </summary>
    public class MetadataMockHttpInterceptor : IHttpRequestInterceptor
    {
        private const string IgdbApiHost = "api.igdb.com";
        private const string SteamApiHost = "store.steampowered.com";

        private readonly Logger _logger;
        private readonly string _mockDataPath;
        private readonly bool _mockEnabled;
        private readonly Dictionary<string, string> _mockDataCache;
        private readonly Dictionary<string, string> _requestBodyCache;

        public MetadataMockHttpInterceptor(Logger logger)
        {
            _logger = logger;
            _mockDataCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _requestBodyCache = new Dictionary<string, string>();
            _mockEnabled = IsMockEnabled();

            // Try to find mock data directory
            _mockDataPath = FindMockDataPath();

            if (_mockEnabled)
            {
                _logger.Info("Metadata mock mode ENABLED. Mock data path: {0}", _mockDataPath ?? "NOT FOUND");
            }
        }

        public HttpRequest PreRequest(HttpRequest request)
        {
            // If mock mode is enabled and this is a request we can mock,
            // mark the request so PostResponse can intercept it
            if (_mockEnabled && CanMockRequest(request))
            {
                var requestId = Guid.NewGuid().ToString();
                request.Headers.Add("X-Gamarr-Mock", "true");
                request.Headers.Add("X-Gamarr-Mock-Id", requestId);

                // Cache the request body since it may be cleared after the HTTP request
                if (request.ContentData != null && request.ContentData.Length > 0)
                {
                    var body = Encoding.UTF8.GetString(request.ContentData);
                    _requestBodyCache[requestId] = body;
                    _logger.Debug("Cached request body for {0}: {1} bytes", request.Url.Host, body.Length);
                }
            }

            return request;
        }

        public HttpResponse PostResponse(HttpResponse response)
        {
            // Check if this request should be mocked
            if (!_mockEnabled || response.Request.Headers.Get("X-Gamarr-Mock") != "true")
            {
                return response;
            }

            // Get the cached request body
            var requestId = response.Request.Headers.Get("X-Gamarr-Mock-Id");
            string cachedBody = null;
            if (!string.IsNullOrEmpty(requestId) && _requestBodyCache.TryGetValue(requestId, out cachedBody))
            {
                _requestBodyCache.Remove(requestId); // Clean up
            }

            var mockData = GetMockData(response.Request, cachedBody);

            if (mockData != null)
            {
                _logger.Debug("Returning mock data for {0}", response.Request.Url);

                var headers = new HttpHeader();
                headers.Add("Content-Type", "application/json");

                return new HttpResponse(response.Request, headers, mockData, HttpStatusCode.OK);
            }

            _logger.Warn("No mock data found for {0}, returning original response", response.Request.Url);
            return response;
        }

        private bool IsMockEnabled()
        {
            // Check both uppercase and lowercase variants due to case-sensitive
            // environment variables on Linux and StringDictionary lowercasing keys
            var envValue = Environment.GetEnvironmentVariable("GAMARR_MOCK_METADATA")
                        ?? Environment.GetEnvironmentVariable("gamarr_mock_metadata");

            return !string.IsNullOrEmpty(envValue) &&
                   (envValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    envValue.Equals("1", StringComparison.OrdinalIgnoreCase));
        }

        private bool CanMockRequest(HttpRequest request)
        {
            var host = request.Url.Host;
            return host.Contains(IgdbApiHost) || host.Contains(SteamApiHost);
        }

        private string GetMockData(HttpRequest request, string cachedBody)
        {
            if (string.IsNullOrEmpty(_mockDataPath))
            {
                return null;
            }

            var host = request.Url.Host;

            if (host.Contains(IgdbApiHost))
            {
                return GetIgdbMockData(request, cachedBody);
            }

            if (host.Contains(SteamApiHost))
            {
                return GetSteamMockData(request);
            }

            return null;
        }

        private string GetIgdbMockData(HttpRequest request, string cachedBody)
        {
            // Use cached body if available, otherwise try to get from request
            var requestBody = cachedBody;
            if (string.IsNullOrEmpty(requestBody) && request.ContentData != null)
            {
                requestBody = Encoding.UTF8.GetString(request.ContentData);
            }

            requestBody = requestBody ?? string.Empty;
            _logger.Debug("IGDB request body length: {0} (from cache: {1})", requestBody.Length, !string.IsNullOrEmpty(cachedBody));

            // Parse the IGDB query to determine what data to return
            // Check for ID lookups: "where id = X" or "where id = (X, Y, Z)"
            var idMatch = Regex.Match(requestBody, @"where\s+id\s*=\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (idMatch.Success)
            {
                var igdbId = idMatch.Groups[1].Value;
                _logger.Debug("IGDB ID lookup detected: {0}", igdbId);
                return LoadMockFile($"igdb_game_{igdbId}.json");
            }

            // Check for search queries - handle various quote types
            var searchMatch = Regex.Match(requestBody, @"search\s+[""\""]([^""\""\n]+)[""\""]", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (searchMatch.Success)
            {
                var rawSearchTerm = searchMatch.Groups[1].Value;
                var searchTerm = rawSearchTerm.ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "");

                _logger.Debug("IGDB search detected: raw='{0}', normalized='{1}'", rawSearchTerm, searchTerm);

                // Try to find a matching search mock file
                var searchFile = $"igdb_search_{searchTerm}.json";
                var data = LoadMockFile(searchFile);

                if (data == null && !string.IsNullOrEmpty(_mockDataPath))
                {
                    // Try partial matches
                    var files = Directory.GetFiles(_mockDataPath, "igdb_search_*.json");
                    _logger.Debug("Looking for partial matches in {0} files", files.Length);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var fileTerm = fileName.Replace("igdb_search_", "").ToLowerInvariant();
                        if (searchTerm.Contains(fileTerm) || fileTerm.Contains(searchTerm))
                        {
                            _logger.Debug("Found partial match: {0}", file);
                            data = LoadMockFile(Path.GetFileName(file));
                            break;
                        }
                    }
                }

                return data;
            }

            _logger.Debug("Could not parse IGDB request body (first 200 chars): {0}",
                requestBody.Length > 200 ? requestBody.Substring(0, 200) : requestBody);
            return null;
        }

        private string GetSteamMockData(HttpRequest request)
        {
            var url = request.Url.ToString();
            _logger.Debug("Steam URL to parse: {0}", url);

            // Check for app details endpoint
            if (url.Contains("appdetails"))
            {
                var appIdMatch = Regex.Match(url, @"appids=(\d+)", RegexOptions.IgnoreCase);
                if (appIdMatch.Success)
                {
                    var appId = appIdMatch.Groups[1].Value;
                    _logger.Debug("Steam appdetails detected for appId: {0}", appId);
                    return LoadMockFile($"steam_app_{appId}.json");
                }
            }

            // Check for store search endpoint
            if (url.Contains("storesearch"))
            {
                var termMatch = Regex.Match(url, @"term=([^&]+)", RegexOptions.IgnoreCase);
                if (termMatch.Success)
                {
                    var rawTerm = termMatch.Groups[1].Value;
                    var searchTerm = Uri.UnescapeDataString(rawTerm)
                        .ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "");

                    _logger.Debug("Steam search detected: raw='{0}', normalized='{1}'", rawTerm, searchTerm);

                    var searchFile = $"steam_search_{searchTerm}.json";
                    var data = LoadMockFile(searchFile);

                    if (data == null)
                    {
                        // Try partial matches
                        var files = Directory.GetFiles(_mockDataPath, "steam_search_*.json");
                        foreach (var file in files)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file);
                            var fileTerm = fileName.Replace("steam_search_", "").ToLowerInvariant();
                            if (searchTerm.Contains(fileTerm) || fileTerm.Contains(searchTerm))
                            {
                                data = LoadMockFile(Path.GetFileName(file));
                                break;
                            }
                        }
                    }

                    return data;
                }
            }

            _logger.Debug("Could not parse Steam URL: {0}", url);
            return null;
        }

        private string LoadMockFile(string fileName)
        {
            if (string.IsNullOrEmpty(_mockDataPath))
            {
                return null;
            }

            var cacheKey = fileName.ToLowerInvariant();

            if (_mockDataCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var filePath = Path.Combine(_mockDataPath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.Debug("Mock data file not found: {0}", filePath);
                return null;
            }

            try
            {
                var content = File.ReadAllText(filePath);
                _mockDataCache[cacheKey] = content;
                _logger.Debug("Loaded mock data from {0}", filePath);
                return content;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load mock data from {0}", filePath);
                return null;
            }
        }

        private string FindMockDataPath()
        {
            // Check environment variable first (both cases for Linux compatibility)
            var envPath = Environment.GetEnvironmentVariable("GAMARR_MOCK_DATA_PATH")
                       ?? Environment.GetEnvironmentVariable("gamarr_mock_data_path");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return envPath;
            }

            // Try common locations relative to the app directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var possiblePaths = new[]
            {
                Path.Combine(appDir, "MockData"),
                Path.Combine(appDir, "Files", "MockData"),
                Path.Combine(appDir, "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(appDir, "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(appDir, "..", "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(appDir, "..", "..", "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(appDir, "..", "..", "..", "..", "src", "NzbDrone.Core.Test", "Files", "MockData")
            };

            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // Try to find it from the source root if running in development
            var sourceRoot = FindSourceRoot(appDir);
            if (!string.IsNullOrEmpty(sourceRoot))
            {
                var srcPath = Path.Combine(sourceRoot, "src", "NzbDrone.Core.Test", "Files", "MockData");
                if (Directory.Exists(srcPath))
                {
                    return srcPath;
                }
            }

            return null;
        }

        private string FindSourceRoot(string startPath)
        {
            var current = startPath;

            while (!string.IsNullOrEmpty(current))
            {
                // Check for .git directory or solution file as indicators of source root
                if (Directory.Exists(Path.Combine(current, ".git")) ||
                    File.Exists(Path.Combine(current, "Gamarr.sln")))
                {
                    return current;
                }

                var parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            return null;
        }
    }
}
