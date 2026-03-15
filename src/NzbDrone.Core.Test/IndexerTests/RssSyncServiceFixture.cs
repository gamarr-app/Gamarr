using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class RssSyncServiceFixture : CoreTest<RssSyncService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IFetchAndParseRss>()
                .Setup(f => f.Fetch())
                .ReturnsAsync(new List<ReleaseInfo>());

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(p => p.GetPending())
                .Returns(new List<ReleaseInfo>());

            Mocker.GetMock<IMakeDownloadDecision>()
                .Setup(d => d.GetRssDecision(It.IsAny<List<ReleaseInfo>>(), It.IsAny<bool>()))
                .Returns(new List<DownloadDecision>());

            Mocker.GetMock<IProcessDownloadDecisions>()
                .Setup(p => p.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                .ReturnsAsync(new ProcessedDecisions(
                    new List<DownloadDecision>(),
                    new List<DownloadDecision>(),
                    new List<DownloadDecision>()));
        }

        [Test]
        public void should_fetch_rss_and_process_decisions()
        {
            Subject.Execute(new RssSyncCommand());

            Mocker.GetMock<IFetchAndParseRss>()
                .Verify(f => f.Fetch(), Times.Once());

            Mocker.GetMock<IMakeDownloadDecision>()
                .Verify(d => d.GetRssDecision(It.IsAny<List<ReleaseInfo>>(), It.IsAny<bool>()), Times.Once());

            Mocker.GetMock<IProcessDownloadDecisions>()
                .Verify(p => p.ProcessDecisions(It.IsAny<List<DownloadDecision>>()), Times.Once());
        }

        [Test]
        public void should_combine_rss_and_pending_releases()
        {
            var rssRelease = new ReleaseInfo { Title = "RSS Release" };
            var pendingRelease = new ReleaseInfo { Title = "Pending Release" };

            Mocker.GetMock<IFetchAndParseRss>()
                .Setup(f => f.Fetch())
                .ReturnsAsync(new List<ReleaseInfo> { rssRelease });

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(p => p.GetPending())
                .Returns(new List<ReleaseInfo> { pendingRelease });

            Subject.Execute(new RssSyncCommand());

            Mocker.GetMock<IMakeDownloadDecision>()
                .Verify(d => d.GetRssDecision(
                    It.Is<List<ReleaseInfo>>(l => l.Count == 2),
                    It.IsAny<bool>()),
                    Times.Once());
        }

        [Test]
        public void should_publish_rss_sync_complete_event()
        {
            Subject.Execute(new RssSyncCommand());

            Mocker.GetMock<IEventAggregator>()
                .Verify(e => e.PublishEvent(It.IsAny<RssSyncCompleteEvent>()), Times.Once());
        }

        [Test]
        public void should_pass_rss_releases_with_no_pending()
        {
            var rssRelease = new ReleaseInfo { Title = "RSS Release" };

            Mocker.GetMock<IFetchAndParseRss>()
                .Setup(f => f.Fetch())
                .ReturnsAsync(new List<ReleaseInfo> { rssRelease });

            Subject.Execute(new RssSyncCommand());

            Mocker.GetMock<IMakeDownloadDecision>()
                .Verify(d => d.GetRssDecision(
                    It.Is<List<ReleaseInfo>>(l => l.Count == 1),
                    It.IsAny<bool>()),
                    Times.Once());
        }

        [Test]
        public void should_work_with_empty_rss_feed()
        {
            Subject.Execute(new RssSyncCommand());

            Mocker.GetMock<IMakeDownloadDecision>()
                .Verify(d => d.GetRssDecision(
                    It.Is<List<ReleaseInfo>>(l => l.Count == 0),
                    It.IsAny<bool>()),
                    Times.Once());
        }
    }
}
