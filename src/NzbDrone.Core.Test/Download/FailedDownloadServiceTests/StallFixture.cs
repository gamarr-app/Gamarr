using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Games;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.FailedDownloadServiceTests
{
    [TestFixture]
    public class StallFixture : CoreTest<FailedDownloadService>
    {
        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            var item = Builder<DownloadClientItem>.CreateNew()
                .With(h => h.Status = DownloadItemStatus.Downloading)
                .With(h => h.OutputPath = new OsPath(@"C:\DropFolder\MyDownload".AsOsAgnostic()))
                .With(h => h.Title = "Some.Game.2026")
                .With(h => h.RemainingSize = 4_000_000_000L)
                .With(h => h.DownloadId = "dl-1")
                .Build();

            _trackedDownload = Builder<TrackedDownload>.CreateNew()
                .With(c => c.State = TrackedDownloadState.Downloading)
                .With(c => c.DownloadItem = item)
                .With(c => c.RemoteGame = new RemoteGame { Game = new Game() })
                .With(c => c.DownloadClient = 7)
                .With(c => c.LastRemainingSize = 4_000_000_000L)
                .With(c => c.RemainingSizeChangedAt = DateTime.UtcNow.AddHours(-10))
                .Build();

            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.Find(_trackedDownload.DownloadItem.DownloadId, GameHistoryEventType.Grabbed))
                .Returns(new List<GameHistory> { Builder<GameHistory>.CreateNew().With(h => h.DownloadId = "dl-1").Build() });
        }

        private void WithStallTimeout(int hours)
        {
            Mocker.GetMock<IDownloadClientFactory>()
                .Setup(f => f.Find(7))
                .Returns(new DownloadClientDefinition { Id = 7, StallTimeoutHours = hours });
        }

        [Test]
        public void should_not_fail_when_stall_check_disabled()
        {
            WithStallTimeout(0);

            Subject.Check(_trackedDownload);

            _trackedDownload.State.Should().Be(TrackedDownloadState.Downloading);
        }

        [Test]
        public void should_fail_when_stalled_past_threshold()
        {
            WithStallTimeout(6);

            Subject.Check(_trackedDownload);

            _trackedDownload.State.Should().Be(TrackedDownloadState.FailedPending);
        }

        [Test]
        public void should_not_fail_when_within_threshold()
        {
            WithStallTimeout(24);

            Subject.Check(_trackedDownload);

            _trackedDownload.State.Should().Be(TrackedDownloadState.Downloading);
        }

        [Test]
        public void should_not_fail_when_already_completed()
        {
            _trackedDownload.DownloadItem.RemainingSize = 0;
            _trackedDownload.DownloadItem.Status = DownloadItemStatus.Completed;
            WithStallTimeout(6);

            Subject.Check(_trackedDownload);

            _trackedDownload.State.Should().Be(TrackedDownloadState.Downloading);
        }

        [Test]
        public void should_only_warn_when_stalled_without_grab_history()
        {
            WithStallTimeout(6);
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.Find(_trackedDownload.DownloadItem.DownloadId, GameHistoryEventType.Grabbed))
                .Returns(new List<GameHistory>());

            Subject.Check(_trackedDownload);

            _trackedDownload.State.Should().Be(TrackedDownloadState.Downloading);
            _trackedDownload.Status.Should().Be(TrackedDownloadStatus.Warning);
        }
    }
}
