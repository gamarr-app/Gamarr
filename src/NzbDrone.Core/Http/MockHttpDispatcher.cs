using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;

namespace NzbDrone.Core.Http
{
    /// <summary>
    /// HTTP dispatcher that returns mock data for IGDB and Steam API calls.
    /// This dispatcher wraps the real dispatcher and intercepts requests to mock URLs
    /// BEFORE any network call is made, allowing tests to run without network access.
    ///
    /// Enable by setting the GAMARR_MOCK_METADATA environment variable to "true".
    /// </summary>
    public class MockHttpDispatcher : IHttpDispatcher
    {
        private const string IgdbApiHost = "api.igdb.com";
        private const string SteamApiHost = "store.steampowered.com";

        private readonly ManagedHttpDispatcher _innerDispatcher;
        private readonly Logger _logger;
        private readonly string _mockDataPath;
        private readonly bool _mockEnabled;
        private readonly Dictionary<string, string> _mockDataCache;

        public MockHttpDispatcher(ManagedHttpDispatcher innerDispatcher, Logger logger)
        {
            _innerDispatcher = innerDispatcher;
            _logger = logger;
            _mockDataCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _mockEnabled = IsMockEnabled();
            _mockDataPath = FindMockDataPath();

            if (_mockEnabled)
            {
                _logger.Info("MockHttpDispatcher: Mock mode ENABLED. Path: {0}", _mockDataPath ?? "NOT FOUND");
            }
        }

        public async Task<HttpResponse> GetResponseAsync(HttpRequest request, CookieContainer cookies)
        {
            // Check if this request should be mocked
            if (_mockEnabled && ShouldMockRequest(request))
            {
                var mockData = GetMockData(request);
                if (mockData != null)
                {
                    _logger.Debug("MockHttpDispatcher: Returning mock data for {0}", request.Url);

                    var headers = new HttpHeader();
                    headers.Add("Content-Type", "application/json");

                    return new HttpResponse(request, headers, mockData, HttpStatusCode.OK);
                }

                // Return empty result instead of falling back to real request
                // This prevents network calls when mock mode is enabled
                _logger.Warn("MockHttpDispatcher: No mock data found for {0}, returning empty response", request.Url);

                var emptyHeaders = new HttpHeader();
                emptyHeaders.Add("Content-Type", "application/json");

                // Return empty array for search results, empty object for single item lookups
                var emptyResponse = request.Url.ToString().Contains("appdetails") ? "{}" : "[]";
                return new HttpResponse(request, emptyHeaders, emptyResponse, HttpStatusCode.OK);
            }

            // Fall back to the real dispatcher
            return await _innerDispatcher.GetResponseAsync(request, cookies);
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

        private bool ShouldMockRequest(HttpRequest request)
        {
            var host = request.Url.Host;
            return host.Contains(IgdbApiHost) || host.Contains(SteamApiHost);
        }

        private string GetMockData(HttpRequest request)
        {
            if (string.IsNullOrEmpty(_mockDataPath))
            {
                return null;
            }

            var host = request.Url.Host;

            if (host.Contains(IgdbApiHost))
            {
                return GetIgdbMockData(request);
            }

            if (host.Contains(SteamApiHost))
            {
                return GetSteamMockData(request);
            }

            return null;
        }

        private string GetIgdbMockData(HttpRequest request)
        {
            var requestBody = request.ContentData != null
                ? Encoding.UTF8.GetString(request.ContentData)
                : string.Empty;

            _logger.Debug("MockHttpDispatcher: IGDB request body length: {0}", requestBody.Length);

            // Check for ID lookups: "where id = X"
            var idMatch = Regex.Match(requestBody, @"where\s+id\s*=\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (idMatch.Success)
            {
                var igdbId = idMatch.Groups[1].Value;
                _logger.Debug("MockHttpDispatcher: IGDB ID lookup: {0}", igdbId);
                return LoadMockFile($"igdb_game_{igdbId}.json");
            }

            // Check for search queries
            var searchMatch = Regex.Match(requestBody, @"search\s+[""\""]([^""\""\n]+)[""\""]", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (searchMatch.Success)
            {
                var rawSearchTerm = searchMatch.Groups[1].Value;
                var searchTerm = rawSearchTerm.ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "");

                _logger.Debug("MockHttpDispatcher: IGDB search: raw='{0}', normalized='{1}'", rawSearchTerm, searchTerm);

                var data = LoadMockFile($"igdb_search_{searchTerm}.json");

                if (data == null)
                {
                    // Try partial matches
                    var files = Directory.GetFiles(_mockDataPath, "igdb_search_*.json");
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var fileTerm = fileName.Replace("igdb_search_", "").ToLowerInvariant();
                        if (searchTerm.Contains(fileTerm) || fileTerm.Contains(searchTerm))
                        {
                            _logger.Debug("MockHttpDispatcher: Found partial match: {0}", file);
                            data = LoadMockFile(Path.GetFileName(file));
                            break;
                        }
                    }
                }

                return data;
            }

            _logger.Debug("MockHttpDispatcher: Could not parse IGDB request");
            return null;
        }

        private string GetSteamMockData(HttpRequest request)
        {
            var url = request.Url.ToString();

            // Check for app details endpoint
            if (url.Contains("appdetails"))
            {
                var appIdMatch = Regex.Match(url, @"appids=(\d+)", RegexOptions.IgnoreCase);
                if (appIdMatch.Success)
                {
                    var appId = appIdMatch.Groups[1].Value;
                    _logger.Debug("MockHttpDispatcher: Steam appdetails: {0}", appId);
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

                    _logger.Debug("MockHttpDispatcher: Steam search: raw='{0}', normalized='{1}'", rawTerm, searchTerm);

                    var data = LoadMockFile($"steam_search_{searchTerm}.json");

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
                                _logger.Debug("MockHttpDispatcher: Found partial match: {0}", file);
                                data = LoadMockFile(Path.GetFileName(file));
                                break;
                            }
                        }
                    }

                    return data;
                }
            }

            _logger.Debug("MockHttpDispatcher: Could not parse Steam URL: {0}", url);
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
                _logger.Debug("MockHttpDispatcher: Mock file not found: {0}", filePath);
                return null;
            }

            try
            {
                var content = File.ReadAllText(filePath);
                _mockDataCache[cacheKey] = content;
                _logger.Debug("MockHttpDispatcher: Loaded mock data from {0}", filePath);
                return content;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MockHttpDispatcher: Failed to load mock data from {0}", filePath);
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

            // Try common locations
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var possiblePaths = new[]
            {
                Path.Combine(appDir, "MockData"),
                Path.Combine(appDir, "Files", "MockData"),
                Path.Combine(appDir, "..", "net8.0", "Files", "MockData"),
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

            // Try to find from source root
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
