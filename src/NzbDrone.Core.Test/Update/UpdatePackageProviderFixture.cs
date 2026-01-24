using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update;

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
            var httpRequestBuilder = new HttpRequestBuilder("https://api.github.com/repos/gamarr-app/Gamarr/releases");

            _requestBuilderFactory.Setup(s => s.Create())
                .Returns(httpRequestBuilder);

            var json = JsonConvert.SerializeObject(releases);

            var httpResponse = new HttpResponse<List<GitHubRelease>>(
                new HttpResponse(
                    new HttpRequest("https://api.github.com/repos/gamarr-app/Gamarr/releases"),
                    new HttpHeader(),
                    json));

            Mocker.GetMock<IHttpClient>()
                .Setup(s => s.Get<List<GitHubRelease>>(It.IsAny<HttpRequest>()))
                .Returns(httpResponse);
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
                    Body = "New release"
                }
            });

            var result = Subject.GetLatestUpdate("main", new Version(1, 0, 0));

            result.Should().NotBeNull();
            result.Version.Should().Be(new Version(2, 0, 0));
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
                    Body = "Beta release"
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
                    Body = "Patch release"
                }
            });

            var result = Subject.GetLatestUpdate("main", new Version(1, 0, 0));

            result.Should().NotBeNull();
            result.Version.Should().Be(new Version(1, 5, 2));
        }
    }
}
