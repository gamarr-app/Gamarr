using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Update
{
    [TestFixture]
    public class UpdatePackageProviderFixture : CoreTest<UpdatePackageProvider>
    {
        private Mock<IHttpRequestBuilderFactory> _requestBuilderFactory;

        [SetUp]
        public void Setup()
        {
            _requestBuilderFactory = new Mock<IHttpRequestBuilderFactory>();

            Mocker.GetMock<IGamarrCloudRequestBuilder>()
                .SetupGet(s => s.Services)
                .Returns(_requestBuilderFactory.Object);
        }

        private void GivenReleases(List<GitHubRelease> releases)
        {
            var json = JsonConvert.SerializeObject(releases);

            GivenReleasesJson(json);
        }

        private void GivenReleasesJson(string json)
        {
            var httpRequestBuilder = new HttpRequestBuilder("https://api.github.com/repos/gamarr-app/Gamarr/releases");

            _requestBuilderFactory.Setup(s => s.Create())
                .Returns(httpRequestBuilder);

            var httpResponse = new HttpResponse<List<GitHubRelease>>(
                new HttpResponse(
                    new HttpRequest("https://api.github.com/repos/gamarr-app/Gamarr/releases"),
                    new HttpHeader(),
                    json));

            Mocker.GetMock<IHttpClient>()
                .Setup(s => s.Get<List<GitHubRelease>>(It.IsAny<HttpRequest>()))
                .Returns(httpResponse);
        }

        private static List<GitHubAsset> RuntimeAssets(string version)
        {
            return new List<GitHubAsset>
            {
                new GitHubAsset
                {
                    Name = $"Gamarr.{version}.{RuntimeInformation.RuntimeIdentifier}.tar.gz",
                    BrowserDownloadUrl = $"https://example.com/Gamarr.{version}.{RuntimeInformation.RuntimeIdentifier}.tar.gz"
                }
            };
        }

        [Test]
        public void should_return_null_when_no_releases_newer_than_current()
        {
            GivenReleases(new List<GitHubRelease>
            {
                new GitHubRelease
                {
                    TagName = "v1.0.0",
                    Prerelease = false,
                    PublishedAt = DateTime.UtcNow,
                    HtmlUrl = "https://github.com/gamarr-app/Gamarr/releases/tag/v1.0.0",
                    Body = "Release notes"
                }
            });

            var result = Subject.GetLatestUpdate("main", new Version(2, 0, 0));

            result.Should().BeNull();
        }

        [Test]
        public void should_return_update_when_newer_release_exists()
        {
            GivenReleases(new List<GitHubRelease>
            {
                new GitHubRelease
                {
                    TagName = "v2.0.0",
                    Prerelease = false,
                    PublishedAt = DateTime.UtcNow,
                    HtmlUrl = "https://github.com/gamarr-app/Gamarr/releases/tag/v2.0.0",
                    Body = "New release",
                    Assets = RuntimeAssets("2.0.0")
                }
            });

            var result = Subject.GetLatestUpdate("main", new Version(1, 0, 0));

            result.Should().NotBeNull();
            result.Version.Should().Be(new Version(2, 0, 0));
            result.Url.Should().Contain(RuntimeInformation.RuntimeIdentifier);
            result.FileName.Should().StartWith("Gamarr.2.0.0.");
        }

        [Test]
        public void should_filter_prereleases_on_main_branch()
        {
            GivenReleases(new List<GitHubRelease>
            {
                new GitHubRelease
                {
                    TagName = "v3.0.0-beta",
                    Prerelease = true,
                    PublishedAt = DateTime.UtcNow,
                    HtmlUrl = "https://github.com/gamarr-app/Gamarr/releases/tag/v3.0.0-beta",
                    Body = "Beta release"
                }
            });

            var result = Subject.GetLatestUpdate("main", new Version(1, 0, 0));

            result.Should().BeNull();
        }

        [Test]
        public void should_include_prereleases_on_develop_branch()
        {
            GivenReleases(new List<GitHubRelease>
            {
                new GitHubRelease
                {
                    TagName = "v3.0.0-beta",
                    Prerelease = true,
                    PublishedAt = DateTime.UtcNow,
                    HtmlUrl = "https://github.com/gamarr-app/Gamarr/releases/tag/v3.0.0-beta",
                    Body = "Beta release",
                    Assets = RuntimeAssets("3.0.0-beta")
                }
            });

            var result = Subject.GetLatestUpdate("develop", new Version(1, 0, 0));

            result.Should().NotBeNull();
            result.Version.Should().Be(new Version(3, 0, 0));
        }

        [Test]
        public void should_handle_version_tags_with_v_prefix()
        {
            GivenReleases(new List<GitHubRelease>
            {
                new GitHubRelease
                {
                    TagName = "v1.5.2",
                    Prerelease = false,
                    PublishedAt = DateTime.UtcNow,
                    HtmlUrl = "https://github.com/gamarr-app/Gamarr/releases/tag/v1.5.2",
                    Body = "Patch release",
                    Assets = RuntimeAssets("1.5.2")
                }
            });

            var result = Subject.GetLatestUpdate("main", new Version(1, 0, 0));

            result.Should().NotBeNull();
            result.Version.Should().Be(new Version(1, 5, 2));
        }

        [Test]
        public void should_parse_github_snake_case_release_json()
        {
            // Regression: TagName/HtmlUrl/PublishedAt previously deserialized to
            // null (no snake_case mapping), so no update was ever detected and
            // System > Updates was always empty.
            var runtime = RuntimeInformation.RuntimeIdentifier;

            GivenReleasesJson($@"[
              {{
                ""tag_name"": ""v99.0.0"",
                ""name"": ""99.0.0"",
                ""body"": ""changes"",
                ""prerelease"": false,
                ""draft"": false,
                ""published_at"": ""2026-07-01T00:00:00Z"",
                ""html_url"": ""https://github.com/gamarr-app/Gamarr/releases/tag/v99.0.0"",
                ""assets"": [
                  {{ ""name"": ""Gamarr.99.0.0.{runtime}.tar.gz"", ""browser_download_url"": ""https://example.com/Gamarr.99.0.0.{runtime}.tar.gz"", ""size"": 123 }},
                  {{ ""name"": ""Gamarr.99.0.0.other-runtime.tar.gz"", ""browser_download_url"": ""https://example.com/other"", ""size"": 123 }}
                ]
              }}
            ]");

            var update = Subject.GetLatestUpdate("main", new Version(1, 0, 0));

            update.Should().NotBeNull();
            update.Version.Should().Be(new Version(99, 0, 0));
            update.FileName.Should().Be($"Gamarr.99.0.0.{runtime}.tar.gz");
            update.Url.Should().Be($"https://example.com/Gamarr.99.0.0.{runtime}.tar.gz");
            update.ReleaseDate.Year.Should().Be(2026);
        }

        [Test]
        public void should_not_offer_update_without_an_asset_for_this_runtime()
        {
            ExceptionVerification.IgnoreWarns();

            GivenReleases(new List<GitHubRelease>
            {
                new GitHubRelease
                {
                    TagName = "v2.0.0",
                    Prerelease = false,
                    PublishedAt = DateTime.UtcNow,
                    HtmlUrl = "https://github.com/gamarr-app/Gamarr/releases/tag/v2.0.0",
                    Body = "New release",
                    Assets = new List<GitHubAsset>
                    {
                        new GitHubAsset { Name = "Gamarr.2.0.0.some-other-runtime.tar.gz", BrowserDownloadUrl = "https://example.com/x" }
                    }
                }
            });

            Subject.GetLatestUpdate("main", new Version(1, 0, 0)).Should().BeNull();
        }
    }
}
