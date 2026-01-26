using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.VirusScanning;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class DownloadedGamesImportServiceFixture : CoreTest<DownloadedGameImportService>
    {
        private string _droneFactory = "c:\\drop\\".AsOsAgnostic();
        private string[] _subFolders = new[] { "c:\\root\\foldername".AsOsAgnostic() };
        private string[] _videoFiles = new[] { "c:\\root\\foldername\\47.ronin.2013.ext".AsOsAgnostic() };

        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IDiskScanService>().Setup(c => c.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(_videoFiles);

            Mocker.GetMock<IDiskScanService>().Setup(c => c.FilterPaths(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()))
                  .Returns<string, IEnumerable<string>, bool>((b, s, e) => s.ToList());

            Mocker.GetMock<IDiskProvider>().Setup(c => c.GetDirectories(It.IsAny<string>()))
                  .Returns(_subFolders);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto))
                  .Returns(new List<ImportResult>());

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), It.IsAny<bool>()))
                  .Returns(new List<ImportDecision>());

            Mocker.GetMock<IReleaseStructureValidator>()
                  .Setup(s => s.ValidateReleaseStructure(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(new ReleaseStructureValidationResult
                  {
                      IsValid = true,
                      SuspiciousFiles = new List<string>(),
                      Confidence = ReleaseStructureConfidence.Unknown
                  });

            Mocker.GetMock<IVirusScannerService>()
                  .Setup(s => s.IsAvailable)
                  .Returns(false);

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(new List<GameHistory>());

            var downloadItem = Builder<DownloadClientItem>.CreateNew()
                .With(v => v.DownloadId = "sab1")
                .With(v => v.Status = DownloadItemStatus.Downloading)
                .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                .With(v => v.Game = new Game())
                .Build();

            _trackedDownload = new TrackedDownload
            {
                DownloadItem = downloadItem,
                RemoteGame = remoteGame,
                State = TrackedDownloadState.Downloading
            };
        }

        private void GivenValidGame()
        {
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns(Builder<Game>.CreateNew().Build());
        }

        private void GivenSuccessfulImport()
        {
            var localGame = new LocalGame();

            var imported = new List<ImportDecision>();
            imported.Add(new ImportDecision(localGame));

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true))
                  .Returns(imported);

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), It.IsAny<bool>(), It.IsAny<DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns(imported.Select(i => new ImportResult(i)).ToList())
                  .Callback(() => WasImportedResponse());
        }

        private void WasImportedResponse()
        {
            Mocker.GetMock<IDiskScanService>().Setup(c => c.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(System.Array.Empty<string>());
        }

        [Test]
        public void should_search_for_series_using_folder_name()
        {
            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IParsingService>().Verify(c => c.GetGame("foldername"), Times.Once());
        }

        [Test]
        public void should_skip_if_file_is_in_use_by_another_process()
        {
            GivenValidGame();

            var filePath = @"C:\media\Game.Title.v1.0.iso".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(filePath))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(filePath))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.IsFileLocked(It.IsAny<string>()))
                  .Returns(true);

            Subject.ProcessPath(filePath);

            VerifyNoImport();
        }

        [Test]
        public void should_skip_if_no_series_found()
        {
            Mocker.GetMock<IParsingService>().Setup(c => c.GetGame("foldername")).Returns((Game)null);

            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(c => c.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), It.IsAny<bool>()),
                    Times.Never());

            VerifyNoImport();
        }

        [Test]
        public void should_not_import_if_folder_is_a_series_path()
        {
            GivenValidGame();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GamePathExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskScanService>()
                  .Setup(c => c.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(System.Array.Empty<string>());

            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IDiskScanService>()
                  .Verify(v => v.GetVideoFiles(It.IsAny<string>(), true), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_delete_folder_if_no_files_were_imported()
        {
            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), false, null, ImportMode.Auto))
                  .Returns(new List<ImportResult>());

            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetFolderSize(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_delete_folder_after_import()
        {
            GivenValidGame();

            GivenSuccessfulImport();

            _trackedDownload.DownloadItem.CanMoveFiles = false;

            Subject.ProcessPath(_droneFactory, ImportMode.Auto, _trackedDownload.RemoteGame.Game, _trackedDownload.DownloadItem);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_delete_folder_if_importmode_move()
        {
            GivenValidGame();

            GivenSuccessfulImport();

            _trackedDownload.DownloadItem.CanMoveFiles = false;

            Subject.ProcessPath(_droneFactory, ImportMode.Move, _trackedDownload.RemoteGame.Game, _trackedDownload.DownloadItem);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Once());
        }

        [Test]
        public void should_not_delete_folder_if_importmode_copy()
        {
            GivenValidGame();

            GivenSuccessfulImport();

            _trackedDownload.DownloadItem.CanMoveFiles = true;

            Subject.ProcessPath(_droneFactory, ImportMode.Copy, _trackedDownload.RemoteGame.Game, _trackedDownload.DownloadItem);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_not_delete_folder_if_files_were_imported_and_video_files_remain()
        {
            GivenValidGame();

            var localGame = new LocalGame();

            var imported = new List<ImportDecision>();
            imported.Add(new ImportDecision(localGame));

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), null, true))
                  .Returns(imported);

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto))
                  .Returns(imported.Select(i => new ImportResult(i)).ToList());

            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_delete_folder_if_files_were_imported_and_only_sample_files_remain()
        {
            GivenValidGame();

            var localGame = new LocalGame();

            var imported = new List<ImportDecision>();
            imported.Add(new ImportDecision(localGame));

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), null, true))
                  .Returns(imported);

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto))
                  .Returns(imported.Select(i => new ImportResult(i)).ToList());

            Mocker.GetMock<IDetectSample>()
                  .Setup(s => s.IsSample(It.IsAny<GameMetadata>(),
                      It.IsAny<string>()))
                  .Returns(DetectSampleResult.Sample);

            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Once());
        }

        [TestCase("_UNPACK_")]
        [TestCase("_FAILED_")]
        public void should_remove_unpack_from_folder_name(string prefix)
        {
            var folderName = "47.ronin.2013.hdtv-lol";
            var folders = new[] { string.Format(@"C:\Test\Unsorted\{0}{1}", prefix, folderName).AsOsAgnostic() };

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.GetDirectories(It.IsAny<string>()))
                  .Returns(folders);

            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IParsingService>()
                .Verify(v => v.GetGame(folderName), Times.Once());

            Mocker.GetMock<IParsingService>()
                .Verify(v => v.GetGame(It.Is<string>(s => s.StartsWith(prefix))), Times.Never());
        }

        [Test]
        public void should_return_importresult_on_unknown_game()
        {
            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            var fileName = @"C:\folder\file.iso".AsOsAgnostic();

            var result = Subject.ProcessPath(fileName);

            result.Should().HaveCount(1);
            result.First().ImportDecision.Should().NotBeNull();
            result.First().ImportDecision.LocalGame.Should().NotBeNull();
            result.First().ImportDecision.LocalGame.Path.Should().Be(fileName);
            result.First().Result.Should().Be(ImportResultType.Rejected);
        }

        [Test]
        public void should_not_delete_if_there_is_large_rar_file()
        {
            GivenValidGame();

            var localGame = new LocalGame();

            var imported = new List<ImportDecision>();
            imported.Add(new ImportDecision(localGame));

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), null, true))
                  .Returns(imported);

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto))
                  .Returns(imported.Select(i => new ImportResult(i)).ToList());

            Mocker.GetMock<IDetectSample>()
                  .Setup(s => s.IsSample(It.IsAny<GameMetadata>(),
                      It.IsAny<string>()))
                  .Returns(DetectSampleResult.Sample);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                  .Returns(new[] { _videoFiles.First().Replace(".ext", ".rar") });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFileSize(It.IsAny<string>()))
                  .Returns(15.Megabytes());

            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_use_folder_if_folder_import()
        {
            GivenValidGame();

            var folderName = @"C:\media\ba09030e-1234-1234-1234-123456789abc\Elden.Ring.v1.10-CODEX".AsOsAgnostic();
            var fileName = @"C:\media\ba09030e-1234-1234-1234-123456789abc\Elden.Ring.v1.10-CODEX\Elden.Ring.v1.10-CODEX.iso".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(folderName))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.GetFiles(folderName, false))
                  .Returns(new[] { fileName });

            var localGame = new LocalGame();

            var imported = new List<ImportDecision>();
            imported.Add(new ImportDecision(localGame));

            Subject.ProcessPath(fileName);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true), Times.Once());
        }

        [Test]
        public void should_not_use_folder_if_file_import()
        {
            GivenValidGame();

            var fileName = @"C:\media\ba09030e-1234-1234-1234-123456789abc\Torrents\Cyberpunk.2077.v2.1-CODEX.iso".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(fileName))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(fileName))
                  .Returns(true);

            var localGame = new LocalGame();

            var imported = new List<ImportDecision>();
            imported.Add(new ImportDecision(localGame));

            var result = Subject.ProcessPath(fileName);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), null, true), Times.Once());
        }

        [Test]
        public void should_not_process_if_file_and_folder_do_not_exist()
        {
            var folderName = @"C:\media\ba09030e-1234-1234-1234-123456789abc\Cyberpunk.2077.v2.1-CODEX".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(folderName))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(folderName))
                  .Returns(false);

            Subject.ProcessPath(folderName).Should().BeEmpty();

            Mocker.GetMock<IParsingService>()
                .Verify(v => v.GetGame(It.IsAny<string>()), Times.Never());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_not_delete_if_no_files_were_imported()
        {
            GivenValidGame();

            var localGame = new LocalGame();

            var imported = new List<ImportDecision>();
            imported.Add(new ImportDecision(localGame));

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), null, true))
                  .Returns(imported);

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto))
                  .Returns(new List<ImportResult>());

            Mocker.GetMock<IDetectSample>()
                  .Setup(s => s.IsSample(It.IsAny<GameMetadata>(),
                      It.IsAny<string>()))
                  .Returns(DetectSampleResult.Sample);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFileSize(It.IsAny<string>()))
                  .Returns(15.Megabytes());

            Subject.ProcessRootFolder(new DirectoryInfo(_droneFactory));

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_return_rejection_if_nothing_imported_and_contains_rar_file()
        {
            GivenValidGame();

            var path = @"C:\media\ba09030e-1234-1234-1234-123456789abc\Elden.Ring.v1.10-CODEX\Elden.Ring.v1.10-CODEX.iso".AsOsAgnostic();
            var imported = new List<ImportDecision>();

            Mocker.GetMock<IMakeImportDecision>()
                .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true))
                .Returns(imported);

            Mocker.GetMock<IImportApprovedGame>()
                .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto))
                .Returns(imported.Select(i => new ImportResult(i)).ToList());

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                .Returns(new[] { _videoFiles.First().Replace(".ext", ".rar") });

            var result = Subject.ProcessPath(path);

            result.Count.Should().Be(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);
        }

        [Test]
        public void should_return_rejection_if_nothing_imported_and_contains_executable_file()
        {
            GivenValidGame();

            var path = @"C:\media\ba09030e-1234-1234-1234-123456789abc\Elden.Ring.v1.10-CODEX\Elden.Ring.v1.10-CODEX.iso".AsOsAgnostic();
            var imported = new List<ImportDecision>();

            Mocker.GetMock<IMakeImportDecision>()
                .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true))
                .Returns(imported);

            Mocker.GetMock<IImportApprovedGame>()
                .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto))
                .Returns(imported.Select(i => new ImportResult(i)).ToList());

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                .Returns(new[] { _videoFiles.First().Replace(".ext", ".exe") });

            var result = Subject.ProcessPath(path);

            result.Count.Should().Be(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);
        }

        private void VerifyNoImport()
        {
            Mocker.GetMock<IImportApprovedGame>().Verify(c => c.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto),
                Times.Never());
        }

        private void VerifyImport()
        {
            Mocker.GetMock<IImportApprovedGame>().Verify(c => c.Import(It.IsAny<List<ImportDecision>>(), true, null, ImportMode.Auto),
                Times.Once());
        }
    }
}
