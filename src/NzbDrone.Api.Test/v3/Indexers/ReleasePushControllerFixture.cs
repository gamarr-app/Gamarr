using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Test.Common;
using Gamarr.Api.V3.Indexers;
using Gamarr.Http.REST;

namespace NzbDrone.Api.Test.v3.Indexers
{
    [TestFixture]
    public class ReleasePushControllerFixture : TestBase<ReleasePushControllerFixture.TestableReleasePushController>
    {
        private const string MagnetUrl = "magnet:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567";

        public class TestableReleasePushController : ReleasePushController
        {
            public TestableReleasePushController(IMakeDownloadDecision downloadDecisionMaker,
                                                 IProcessDownloadDecisions downloadDecisionProcessor,
                                                 IIndexerFactory indexerFactory,
                                                 IDownloadClientFactory downloadClientFactory,
                                                 IQualityProfileService qualityProfileService,
                                                 Logger logger)
                : base(downloadDecisionMaker, downloadDecisionProcessor, indexerFactory, downloadClientFactory, qualityProfileService, logger)
            {
            }

            public ResourceValidator<ReleaseResource> Validator => PostValidator;
        }

        private static ReleaseResource Release(string downloadUrl, string magnetUrl, DownloadProtocol protocol)
        {
            return new ReleaseResource
            {
                Title = "Some.Game.2026-GROUP",
                PublishDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                DownloadUrl = downloadUrl,
                MagnetUrl = magnetUrl,
                Protocol = protocol
            };
        }

        [Test]
        public void should_accept_a_release_that_only_has_a_magnet_url()
        {
            var result = Subject.Validator.Validate(Release(null, MagnetUrl, DownloadProtocol.Torrent));

            result.IsValid.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }

        [Test]
        public void should_accept_a_release_that_only_has_a_download_url()
        {
            var result = Subject.Validator.Validate(Release("https://indexer.invalid/x.torrent", null, DownloadProtocol.Torrent));

            result.IsValid.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }

        [Test]
        public void should_reject_a_release_with_neither_a_download_url_nor_a_magnet_url()
        {
            var result = Subject.Validator.Validate(Release(null, null, DownloadProtocol.Torrent));

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.PropertyName).Should().Contain("DownloadUrl");
            result.Errors.Select(e => e.PropertyName).Should().Contain("MagnetUrl");
        }

        [Test]
        public void should_reject_a_magnet_only_release_that_is_not_sent_as_a_torrent()
        {
            // ToModel drops MagnetUrl for anything but the torrent protocol, so accepting this
            // would hand an empty DownloadUrl to the usenet grab path instead of failing here.
            var result = Subject.Validator.Validate(Release(null, MagnetUrl, DownloadProtocol.Usenet));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Protocol");
        }

        [Test]
        public void should_still_accept_a_usenet_release_that_has_a_download_url()
        {
            var result = Subject.Validator.Validate(Release("https://indexer.invalid/x.nzb", null, DownloadProtocol.Usenet));

            result.IsValid.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }

        [Test]
        public void should_derive_the_guid_from_the_magnet_url_when_there_is_no_download_url()
        {
            var pushed = CapturePushedRelease(Release(null, MagnetUrl, DownloadProtocol.Torrent));

            pushed.Guid.Should().Be("PUSH-" + MagnetUrl);
        }

        [Test]
        public void should_carry_the_magnet_url_onto_the_torrent_info()
        {
            var pushed = CapturePushedRelease(Release(null, MagnetUrl, DownloadProtocol.Torrent));

            pushed.Should().BeOfType<TorrentInfo>();
            ((TorrentInfo)pushed).MagnetUrl.Should().Be(MagnetUrl);
        }

        [Test]
        public void should_keep_deriving_the_guid_from_the_download_url_when_one_is_present()
        {
            var pushed = CapturePushedRelease(Release("https://indexer.invalid/x.torrent", MagnetUrl, DownloadProtocol.Torrent));

            pushed.Guid.Should().Be("PUSH-https://indexer.invalid/x.torrent");
        }

        private ReleaseInfo CapturePushedRelease(ReleaseResource resource)
        {
            ReleaseInfo captured = null;

            Mocker.GetMock<IMakeDownloadDecision>()
                .Setup(s => s.GetRssDecision(It.IsAny<List<ReleaseInfo>>(), true))
                .Callback<List<ReleaseInfo>, bool>((reports, _) => captured = reports.Single())
                .Returns(new List<DownloadDecision>());

            Subject.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            Subject.ControllerContext.HttpContext.Request.Method = "POST";
            Subject.ControllerContext.HttpContext.Request.Path = "/api/v3/release/push";

            // No decisions come back from the stubbed decision maker, so Create always ends in the
            // "Unable to parse" failure. The release it built on the way there is what we're after.
            Assert.Throws<ValidationException>(() => Subject.Create(resource));

            captured.Should().NotBeNull();

            return captured;
        }
    }
}
