using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.TrackedDownloads
{
    [TestFixture]
    public class DownloadMonitoringServiceFixture : CoreTest<DownloadMonitoringService>
    {
        private Mock<IDownloadClient> _client;

        [SetUp]
        public void Setup()
        {
            _client = new Mock<IDownloadClient>();
            _client.SetupGet(c => c.Definition).Returns(new DownloadClientDefinition { Id = 1, Name = "aria2" });

            Mocker.GetMock<IDownloadClientFactory>()
                  .Setup(s => s.DownloadHandlingEnabled(It.IsAny<bool>()))
                  .Returns(new List<IDownloadClient> { _client.Object });
        }

        [Test]
        public void should_skip_items_without_a_download_id()
        {
            _client.Setup(c => c.GetItems()).Returns(new List<DownloadClientItem>
            {
                new DownloadClientItem { DownloadId = null, Title = "plain http download" },
                new DownloadClientItem { DownloadId = "ABC", Title = "Some.Game-GROUP" }
            });

            Subject.Execute(new RefreshMonitoredDownloadsCommand());

            Mocker.GetMock<ITrackedDownloadService>()
                  .Verify(v => v.TrackDownload(It.IsAny<DownloadClientDefinition>(), It.Is<DownloadClientItem>(i => i.DownloadId == null)), Times.Never());

            Mocker.GetMock<ITrackedDownloadService>()
                  .Verify(v => v.TrackDownload(It.IsAny<DownloadClientDefinition>(), It.Is<DownloadClientItem>(i => i.DownloadId == "ABC")), Times.Once());

            Mocker.GetMock<ITrackedDownloadService>()
                  .Verify(v => v.StopTrackingMissing(1, It.Is<List<string>>(l => l.SequenceEqual(new[] { "ABC" }))), Times.Once());
        }
    }
}
