using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
                var request = _requestBuilder.Create()
                                             .Resource("releases")
                                             .Build();

                var releases = _httpClient.Get<List<GitHubRelease>>(request).Resource;

                if (releases == null || !releases.Any())
                {
                    return null;
                }

                // Find the latest release (not prerelease unless on develop branch)
                var release = branch.Equals("develop", StringComparison.OrdinalIgnoreCase)
                    ? releases.FirstOrDefault()
                    : releases.FirstOrDefault(r => !r.Prerelease);

                if (release == null)
                {
                    return null;
                }

                var releaseVersion = ParseVersion(release.TagName);
                if (releaseVersion == null || releaseVersion <= currentVersion)
                {
                    return null;
                }

                return new UpdatePackage
                {
                    Version = releaseVersion,
                    Branch = branch,
                    ReleaseDate = release.PublishedAt,
                    FileName = release.TagName,
                    Url = release.HtmlUrl,
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
                var request = _requestBuilder.Create()
                                             .Resource("releases")
                                             .Build();

                var releases = _httpClient.Get<List<GitHubRelease>>(request).Resource;

                if (releases == null || !releases.Any())
                {
                    return new List<UpdatePackage>();
                }

                return releases
                    .Where(r => !r.Prerelease || branch.Equals("develop", StringComparison.OrdinalIgnoreCase))
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

    public class GitHubRelease
    {
        public string TagName { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public bool Prerelease { get; set; }
        public bool Draft { get; set; }
        public DateTime PublishedAt { get; set; }
        public string HtmlUrl { get; set; }
    }
}
