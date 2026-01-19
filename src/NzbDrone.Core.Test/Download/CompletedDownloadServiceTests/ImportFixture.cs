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
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class ImportFixture : CoreTest<CompletedDownloadService>
    {
        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            var completed = Builder<DownloadClientItem>.CreateNew()
                                                    .With(h => h.Status = DownloadItemStatus.Completed)
                                                    .With(h => h.OutputPath = new OsPath(@"C:\DropFolder\MyDownload".AsOsAgnostic()))
                                                    .With(h => h.Title = "Witcher3.GOTY.v4.0-CODEX")
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

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.MostRecentForDownloadId(_trackedDownload.DownloadItem.DownloadId))
                  .Returns(new GameHistory());

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame("Witcher3.GOTY.v4.0-CODEX"))
                  .Returns(remoteGame.Game);

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(new List<GameHistory>());

            Mocker.GetMock<IProvideImportItemService>()
                  .Setup(s => s.ProvideImportItem(It.IsAny<DownloadClientItem>(), It.IsAny<DownloadClientItem>()))
                  .Returns<DownloadClientItem, DownloadClientItem>((i, p) => i);
        }

        private RemoteGame BuildRemoteGame()
        {
            return new RemoteGame
            {
                Game = new Game()
            };
        }

        private void GivenABadlyNamedDownload()
        {
            _trackedDownload.DownloadItem.DownloadId = "1234";
            _trackedDownload.DownloadItem.Title = "Elden Ring"; // Set a badly named download
            Mocker.GetMock<IHistoryService>()
               .Setup(s => s.MostRecentForDownloadId(It.Is<string>(i => i == "1234")))
               .Returns(new GameHistory() { SourceTitle = "Elden.Ring.v1.10-CODEX" });

            Mocker.GetMock<IParsingService>()
               .Setup(s => s.GetGame(It.IsAny<string>()))
               .Returns((Game)null);

            Mocker.GetMock<IParsingService>()
                .Setup(s => s.GetGame("Elden.Ring.v1.10-CODEX"))
                .Returns(BuildRemoteGame().Game);
        }

        private void GivenSeriesMatch()
        {
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns(_trackedDownload.RemoteGame.Game);
        }

        [Test]
        public void should_not_mark_as_imported_if_all_files_were_rejected()
        {
            Mocker.GetMock<IDownloadedGameImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(
                                   new ImportDecision(
                                       new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso" }, new ImportRejection(ImportRejectionReason.Unknown, "Rejected!")), "Test Failure"),

                               new ImportResult(
                                   new ImportDecision(
                                       new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.11.iso" }, new ImportRejection(ImportRejectionReason.Unknown, "Rejected!")), "Test Failure")
                           });

            Subject.Import(_trackedDownload);

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent<DownloadCompletedEvent>(It.IsAny<DownloadCompletedEvent>()), Times.Never());

            AssertNotImported();
        }

        [Test]
        public void should_not_mark_as_imported_if_no_games_were_parsed()
        {
            Mocker.GetMock<IDownloadedGameImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(
                                   new ImportDecision(
                                       new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso" }, new ImportRejection(ImportRejectionReason.Unknown, "Rejected!")), "Test Failure"),

                               new ImportResult(
                                   new ImportDecision(
                                       new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso" }, new ImportRejection(ImportRejectionReason.Unknown, "Rejected!")), "Test Failure")
                           });

            _trackedDownload.RemoteGame.Game = new Game();

            Subject.Import(_trackedDownload);

            AssertNotImported();
        }

        [Test]
        public void should_not_mark_as_imported_if_all_files_were_skipped()
        {
            Mocker.GetMock<IDownloadedGameImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(new ImportDecision(new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso" }), "Test Failure"),
                               new ImportResult(new ImportDecision(new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso" }), "Test Failure")
                           });

            Subject.Import(_trackedDownload);

            AssertNotImported();
        }

        [Test]
        public void should_mark_as_imported_if_all_games_were_imported_but_extra_files_were_not()
        {
            GivenSeriesMatch();

            _trackedDownload.RemoteGame.Game = new Game();

            Mocker.GetMock<IDownloadedGameImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
               {
                               new ImportResult(new ImportDecision(new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso", Game = _trackedDownload.RemoteGame.Game })),
                               new ImportResult(new ImportDecision(new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso" }), "Test Failure")
               });

            Subject.Import(_trackedDownload);

            AssertImported();
        }

        [Test]
        public void should_mark_as_imported_if_the_download_can_be_tracked_using_the_source_gameid()
        {
            GivenABadlyNamedDownload();

            Mocker.GetMock<IDownloadedGameImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
               {
                               new ImportResult(new ImportDecision(new LocalGame { Path = @"C:\TestPath\Elden.Ring.v1.10.iso", Game = _trackedDownload.RemoteGame.Game }))
               });

            Mocker.GetMock<IGameService>()
                  .Setup(v => v.GetGame(It.IsAny<int>()))
                  .Returns(BuildRemoteGame().Game);

            Subject.Import(_trackedDownload);

            AssertImported();
        }

        private void AssertNotImported()
        {
            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<DownloadCompletedEvent>()), Times.Never());

            _trackedDownload.State.Should().Be(TrackedDownloadState.ImportBlocked);
        }

        private void AssertImported()
        {
            Mocker.GetMock<IDownloadedGameImportService>()
                .Verify(v => v.ProcessPath(_trackedDownload.DownloadItem.OutputPath.FullPath, ImportMode.Auto, _trackedDownload.RemoteGame.Game, _trackedDownload.DownloadItem), Times.Once());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<DownloadCompletedEvent>()), Times.Once());

            _trackedDownload.State.Should().Be(TrackedDownloadState.Imported);
        }
    }
}
