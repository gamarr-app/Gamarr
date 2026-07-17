using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Update
{
    public interface IUpdatePackageProvider
    {
        UpdatePackage GetLatestUpdate(string branch, Version currentVersion);
        List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion = null);
    }

    public class UpdatePackageProvider : IUpdatePackageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IHttpRequestBuilderFactory _requestBuilder;
        private readonly Logger _logger;

        public UpdatePackageProvider(IHttpClient httpClient, IGamarrCloudRequestBuilder requestBuilder, IAnalyticsService analyticsService, IPlatformInfo platformInfo, IMainDatabase mainDatabase, Logger logger)
        {
            _requestBuilder = requestBuilder.Services;
            _httpClient = httpClient;
            _logger = logger;
        }

        public UpdatePackage GetLatestUpdate(string branch, Version currentVersion)
        {
            try
            {
                var releases = GetReleases();

                if (releases == null || !releases.Any())
                {
                    return null;
                }

                // Find the latest release (not prerelease unless on develop branch)
                var release = branch.Equals("develop", StringComparison.OrdinalIgnoreCase)
                    ? releases.FirstOrDefault(r => !r.Draft)
                    : releases.FirstOrDefault(r => !r.Draft && !r.Prerelease);

                if (release == null)
                {
                    return null;
                }

                var releaseVersion = ParseVersion(release.TagName);
                if (releaseVersion == null || releaseVersion <= currentVersion)
                {
                    return null;
                }

                // Pick the archive built for this runtime (assets are named
                // Gamarr.{version}.{runtime}.tar.gz / .zip).
                var runtime = RuntimeInformation.RuntimeIdentifier;
                var asset = release.Assets?.FirstOrDefault(a => a.Name?.Contains($".{runtime}.", StringComparison.OrdinalIgnoreCase) == true &&
                                                                !a.Name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase));

                if (asset == null)
                {
                    _logger.Warn("Update {0} is available but has no package for runtime '{1}'", releaseVersion, runtime);
                    return null;
                }

                return new UpdatePackage
                {
                    Version = releaseVersion,
                    Branch = branch,
                    ReleaseDate = release.PublishedAt,
                    FileName = asset.Name,
                    Url = asset.BrowserDownloadUrl,
                    Hash = GetAssetHash(release, asset),
                    Changes = new UpdateChanges { New = new List<string> { release.Body } }
                };
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to check for updates from GitHub");
                return null;
            }
        }

        public List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion)
        {
            try
            {
                var releases = GetReleases();

                if (releases == null || !releases.Any())
                {
                    return new List<UpdatePackage>();
                }

                return releases
                    .Where(r => !r.Draft && (!r.Prerelease || branch.Equals("develop", StringComparison.OrdinalIgnoreCase)))
                    .Select(r => new UpdatePackage
                    {
                        Version = ParseVersion(r.TagName),
                        Branch = branch,
                        ReleaseDate = r.PublishedAt,
                        FileName = r.TagName,
                        Url = r.HtmlUrl,
                        Changes = new UpdateChanges { New = new List<string> { r.Body } }
                    })
                    .Where(p => p.Version != null)
                    .Take(10)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to get recent updates from GitHub");
                return new List<UpdatePackage>();
            }
        }

        private List<GitHubRelease> GetReleases()
        {
            var request = _requestBuilder.Create()
                                         .Resource("releases")
                                         .Build();

            return _httpClient.Get<List<GitHubRelease>>(request).Resource;
        }

        private string GetAssetHash(GitHubRelease release, GitHubAsset asset)
        {
            // Releases publish a {asset}.sha256 sidecar ("<hash>  <filename>");
            // older releases don't have one — verification is skipped for those.
            var checksumAsset = release.Assets?.FirstOrDefault(a =>
                a.Name?.Equals($"{asset.Name}.sha256", StringComparison.OrdinalIgnoreCase) == true);

            if (checksumAsset == null)
            {
                return null;
            }

            try
            {
                var content = _httpClient.Get(new HttpRequest(checksumAsset.BrowserDownloadUrl)).Content;

                return content?.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to fetch checksum for update package {0}", asset.Name);
                return null;
            }
        }

        private Version ParseVersion(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                return null;
            }

            // Remove 'v' prefix if present
            var versionString = tagName.TrimStart('v', 'V');

            // Try to parse as version
            if (Version.TryParse(versionString, out var version))
            {
                return version;
            }

            // Try to extract version from tag like "v1.0.0-beta"
            var match = Regex.Match(versionString, @"^(\d+\.\d+\.\d+)");
            if (match.Success && Version.TryParse(match.Groups[1].Value, out version))
            {
                return version;
            }

            return null;
        }
    }

    // GitHub's REST API serializes with snake_case; explicit mappings are
    // required (the default resolver is not underscore-insensitive, so
    // TagName/HtmlUrl/PublishedAt silently deserialized to null before).
    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        public string Name { get; set; }
        public string Body { get; set; }
        public bool Prerelease { get; set; }
        public bool Draft { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; }
    }

    public class GitHubAsset
    {
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        public long Size { get; set; }
    }
}
