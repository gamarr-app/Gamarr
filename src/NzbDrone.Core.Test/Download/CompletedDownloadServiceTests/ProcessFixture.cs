using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.CompletedDownloadServiceTests
{
    [TestFixture]
    public class ProcessFixture : CoreTest<CompletedDownloadService>
    {
        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            var completed = Builder<DownloadClientItem>.CreateNew()
                                                    .With(h => h.Status = DownloadItemStatus.Completed)
                                                    .With(h => h.OutputPath = new OsPath(@"C:\DropFolder\MyDownload".AsOsAgnostic()))
                                                    .With(h => h.Title = "Cyberpunk.2077.v2.1-CODEX")
                                                    .Build();

            var remoteGame = BuildRemoteGame();

            _trackedDownload = Builder<TrackedDownload>.CreateNew()
                    .With(c => c.State = TrackedDownloadState.Downloading)
                    .With(c => c.DownloadItem = completed)
                    .With(c => c.RemoteGame = remoteGame)
                    .Build();

            Mocker.GetMock<IDownloadClient>()
              .SetupGet(c => c.Definition)
              .Returns(new DownloadClientDefinition { Id = 1, Name = "testClient" });

            Mocker.GetMock<IProvideDownloadClient>()
                  .Setup(c => c.Get(It.IsAny<int>()))
                  .Returns(Mocker.GetMock<IDownloadClient>().Object);

            Mocker.GetMock<IProvideImportItemService>()
                  .Setup(c => c.ProvideImportItem(It.IsAny<DownloadClientItem>(), It.IsAny<DownloadClientItem>()))
                  .Returns((DownloadClientItem item, DownloadClientItem previous) => item);

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(_trackedDownload.DownloadItem.DownloadId))
                  .Returns(new List<GameHistory>());

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame("Cyberpunk.2077.v2.1-CODEX"))
                  .Returns(remoteGame.Game);
        }

        private RemoteGame BuildRemoteGame()
        {
            return new RemoteGame
            {
                Game = new Game(),
            };
        }

        private void GivenNoGrabbedHistory()
        {
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.FindByDownloadId(_trackedDownload.DownloadItem.DownloadId))
                .Returns(new List<GameHistory>());
        }

        private void GivenGameMatch()
        {
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns(_trackedDownload.RemoteGame.Game);
        }

        private void GivenABadlyNamedDownload()
        {
            _trackedDownload.DownloadItem.DownloadId = "1234";
            _trackedDownload.DownloadItem.Title = "Elden Ring"; // Set a badly named download
            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.Is<string>(i => i == "1234")))
                  .Returns(new List<GameHistory>
                  {
                      new GameHistory() { SourceTitle = "Elden.Ring.v1.10-CODEX", EventType = GameHistoryEventType.Grabbed }
                  });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns((Game)null);

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame("Elden.Ring.v1.10-CODEX"))
                  .Returns(BuildRemoteGame().Game);
        }

        [TestCase(DownloadItemStatus.Downloading)]
        [TestCase(DownloadItemStatus.Failed)]
        [TestCase(DownloadItemStatus.Queued)]
        [TestCase(DownloadItemStatus.Paused)]
        [TestCase(DownloadItemStatus.Warning)]
        public void should_not_process_if_download_status_isnt_completed(DownloadItemStatus status)
        {
            _trackedDownload.DownloadItem.Status = status;

            Subject.Check(_trackedDownload);

            AssertNotReadyToImport();
        }

        [Test]
        public void should_not_process_if_matching_history_is_not_found_and_no_category_specified()
        {
            _trackedDownload.DownloadItem.Category = null;
            GivenNoGrabbedHistory();

            Subject.Check(_trackedDownload);

            AssertNotReadyToImport();
        }

        [Test]
        public void should_process_if_matching_history_is_not_found_but_category_specified()
        {
            _trackedDownload.DownloadItem.Category = "tv";
            GivenNoGrabbedHistory();
            GivenGameMatch();

            Subject.Check(_trackedDownload);

            AssertReadyToImport();
        }

        [Test]
        public void should_not_process_if_output_path_is_empty()
        {
            _trackedDownload.DownloadItem.OutputPath = default(OsPath);

            Subject.Check(_trackedDownload);

            AssertNotReadyToImport();
        }

        [Test]
        public void should_not_process_if_the_download_cannot_be_tracked_using_the_source_title_as_it_was_initiated_externally()
        {
            GivenABadlyNamedDownload();

            Mocker.GetMock<IDownloadedGameImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(new ImportDecision(new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso" }))
                           });

            Subject.Check(_trackedDownload);

            AssertNotReadyToImport();
        }

        [Test]
        public void should_not_process_when_there_is_a_title_mismatch()
        {
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame("Cyberpunk.2077.v2.1-CODEX"))
                  .Returns((Game)null);

            Subject.Check(_trackedDownload);

            AssertNotReadyToImport();
        }

        private void AssertNotReadyToImport()
        {
            _trackedDownload.State.Should().NotBe(TrackedDownloadState.ImportPending);
        }

        private void AssertReadyToImport()
        {
            _trackedDownload.State.Should().Be(TrackedDownloadState.ImportPending);
        }
    }
}
