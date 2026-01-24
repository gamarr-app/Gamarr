using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.VirusScanning;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport
{
    [TestFixture]
    public class DownloadedGameImportServiceFixture : CoreTest<DownloadedGameImportService>
    {
        private string _folderPath = @"C:\downloads\Game.Title.v1.0-GROUP".AsOsAgnostic();
        private string _filePath = @"C:\downloads\Game.Title.v1.0-GROUP\game.iso".AsOsAgnostic();
        private Game _game;
        private DownloadClientItem _downloadClientItem;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                .With(g => g.Path = @"C:\games\Game Title".AsOsAgnostic())
                .With(g => g.GameMetadata = new GameMetadata { Title = "Game Title" })
                .Build();

            _downloadClientItem = Builder<DownloadClientItem>.CreateNew()
                .With(d => d.DownloadId = "download1")
                .With(d => d.CanMoveFiles = true)
                .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(It.IsAny<string>()))
                  .Returns(new[] { _folderPath });

            Mocker.GetMock<IDiskScanService>()
                  .Setup(s => s.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(System.Array.Empty<string>());

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GamePathExists(It.IsAny<string>()))
                  .Returns(false);

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

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), It.IsAny<bool>(), It.IsAny<DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns(new List<ImportResult>());
        }

        private void GivenGameParsedFromFolder()
        {
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns(_game);
        }

        private void GivenSuccessfulFolderImport()
        {
            var localGame = new LocalGame { Path = _folderPath, Game = _game };
            var decisions = new List<ImportDecision> { new ImportDecision(localGame) };

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true))
                  .Returns(decisions);

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), It.IsAny<bool>(), It.IsAny<DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns(decisions.Select(d => new ImportResult(d)).ToList());
        }

        [Test]
        public void should_process_as_folder_when_path_is_folder()
        {
            GivenGameParsedFromFolder();

            Subject.ProcessPath(_folderPath, ImportMode.Auto, null, _downloadClientItem);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Contains(_folderPath)), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true), Times.Once());
        }

        [Test]
        public void should_process_as_file_when_path_is_file()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_filePath))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(_filePath))
                  .Returns(true);

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns(_game);

            Subject.ProcessPath(_filePath, ImportMode.Auto, null, _downloadClientItem);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Contains(_filePath)), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true), Times.Once());
        }

        [Test]
        public void should_return_unknown_game_result_when_folder_not_matched()
        {
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns((Game)null);

            var result = Subject.ProcessPath(_folderPath, ImportMode.Auto, null, _downloadClientItem);

            result.Should().HaveCount(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);
        }

        [Test]
        public void should_reject_when_folder_is_mapped_to_existing_game()
        {
            GivenGameParsedFromFolder();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GamePathExists(_folderPath))
                  .Returns(true);

            var result = Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            result.Should().HaveCount(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_reject_when_suspicious_files_detected()
        {
            GivenGameParsedFromFolder();

            Mocker.GetMock<IReleaseStructureValidator>()
                  .Setup(s => s.ValidateReleaseStructure(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(new ReleaseStructureValidationResult
                  {
                      IsValid = false,
                      SuspiciousFiles = new List<string> { "trojan.exe", "malware.bat" },
                      Confidence = ReleaseStructureConfidence.High
                  });

            var result = Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            result.Should().HaveCount(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_reject_when_release_structure_invalid_with_high_confidence()
        {
            GivenGameParsedFromFolder();

            Mocker.GetMock<IReleaseStructureValidator>()
                  .Setup(s => s.ValidateReleaseStructure(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(new ReleaseStructureValidationResult
                  {
                      IsValid = false,
                      SuspiciousFiles = new List<string>(),
                      Confidence = ReleaseStructureConfidence.High,
                      Message = "Structure mismatch"
                  });

            var result = Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            result.Should().HaveCount(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_reject_when_release_structure_invalid_with_low_confidence()
        {
            GivenGameParsedFromFolder();
            GivenSuccessfulFolderImport();

            Mocker.GetMock<IReleaseStructureValidator>()
                  .Setup(s => s.ValidateReleaseStructure(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(new ReleaseStructureValidationResult
                  {
                      IsValid = false,
                      SuspiciousFiles = new List<string>(),
                      Confidence = ReleaseStructureConfidence.Low,
                      Message = "Structure mismatch"
                  });

            var result = Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            result.Should().NotBeEmpty();

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true), Times.Once());
        }

        [Test]
        public void should_reject_when_virus_detected()
        {
            GivenGameParsedFromFolder();

            Mocker.GetMock<IVirusScannerService>()
                  .Setup(s => s.IsAvailable)
                  .Returns(true);

            Mocker.GetMock<IVirusScannerService>()
                  .Setup(s => s.ScanPath(It.IsAny<string>()))
                  .Returns(new VirusScanResult
                  {
                      ScanCompleted = true,
                      IsClean = false,
                      InfectedFiles = new List<InfectedFile>
                      {
                          new InfectedFile { FilePath = "setup.exe", ThreatName = "Trojan.Generic" }
                      }
                  });

            var result = Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            result.Should().HaveCount(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_quarantine_folder_when_virus_detected_and_quarantine_enabled()
        {
            GivenGameParsedFromFolder();

            var quarantineFolder = @"C:\quarantine".AsOsAgnostic();

            Mocker.GetMock<IVirusScannerService>()
                  .Setup(s => s.IsAvailable)
                  .Returns(true);

            Mocker.GetMock<IVirusScannerService>()
                  .Setup(s => s.ScanPath(It.IsAny<string>()))
                  .Returns(new VirusScanResult
                  {
                      ScanCompleted = true,
                      IsClean = false,
                      InfectedFiles = new List<InfectedFile>
                      {
                          new InfectedFile { FilePath = "trojan.exe", ThreatName = "Win32.Malware" }
                      }
                  });

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.QuarantineInfectedFiles)
                  .Returns(true);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.QuarantineFolder)
                  .Returns(quarantineFolder);

            Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.MoveFolder(It.IsAny<string>(), It.Is<string>(s => s.StartsWith(quarantineFolder)), It.IsAny<bool>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_not_scan_virus_when_scanner_not_available()
        {
            GivenGameParsedFromFolder();
            GivenSuccessfulFolderImport();

            Mocker.GetMock<IVirusScannerService>()
                  .Setup(s => s.IsAvailable)
                  .Returns(false);

            Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            Mocker.GetMock<IVirusScannerService>()
                  .Verify(v => v.ScanPath(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_pass_folder_path_to_import_decision_maker_for_game_folder()
        {
            GivenGameParsedFromFolder();
            GivenSuccessfulFolderImport();

            Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(
                      It.Is<List<string>>(l => l.Count == 1 && l.First() == _folderPath),
                      _game,
                      _downloadClientItem,
                      It.IsAny<ParsedGameInfo>(),
                      true), Times.Once());
        }

        [Test]
        public void should_delete_folder_after_successful_import_in_move_mode()
        {
            GivenGameParsedFromFolder();
            GivenSuccessfulFolderImport();

            Mocker.GetMock<IDiskScanService>()
                  .Setup(s => s.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(System.Array.Empty<string>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                  .Returns(System.Array.Empty<string>());

            Subject.ProcessPath(_folderPath, ImportMode.Move, _game, _downloadClientItem);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Once());
        }

        [Test]
        public void should_not_delete_folder_after_successful_import_in_copy_mode()
        {
            GivenGameParsedFromFolder();
            GivenSuccessfulFolderImport();

            Subject.ProcessPath(_folderPath, ImportMode.Copy, _game, _downloadClientItem);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_check_for_dangerous_files_when_import_result_is_empty()
        {
            GivenGameParsedFromFolder();

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true))
                  .Returns(new List<ImportDecision>());

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), It.IsAny<bool>(), It.IsAny<DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns(new List<ImportResult>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                  .Returns(new[] { @"C:\downloads\Game\setup.exe".AsOsAgnostic() });

            var result = Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            result.Should().HaveCount(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);
        }

        [Test]
        public void should_detect_archive_files_when_import_result_is_empty()
        {
            GivenGameParsedFromFolder();

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true))
                  .Returns(new List<ImportDecision>());

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), It.IsAny<bool>(), It.IsAny<DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns(new List<ImportResult>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                  .Returns(new[] { @"C:\downloads\Game\archive.rar".AsOsAgnostic() });

            var result = Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            result.Should().HaveCount(1);
            result.First().Result.Should().Be(ImportResultType.Rejected);
        }

        [Test]
        public void should_return_empty_when_path_does_not_exist()
        {
            var invalidPath = @"C:\nonexistent\path".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(invalidPath))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(invalidPath))
                  .Returns(false);

            var result = Subject.ProcessPath(invalidPath);

            result.Should().BeEmpty();

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_strip_unpack_prefix_from_folder_name()
        {
            var folderPath = @"C:\downloads\_UNPACK_Game.Title.v1.0-GROUP".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(It.IsAny<string>()))
                  .Returns(new[] { folderPath });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ProcessRootFolder(new DirectoryInfo(@"C:\downloads\".AsOsAgnostic()));

            Mocker.GetMock<IParsingService>()
                  .Verify(v => v.GetGame("Game.Title.v1.0-GROUP"), Times.Once());
        }

        [Test]
        public void should_strip_failed_prefix_from_folder_name()
        {
            var folderPath = @"C:\downloads\_FAILED_Game.Title.v1.0-GROUP".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(It.IsAny<string>()))
                  .Returns(new[] { folderPath });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ProcessRootFolder(new DirectoryInfo(@"C:\downloads\".AsOsAgnostic()));

            Mocker.GetMock<IParsingService>()
                  .Verify(v => v.GetGame("Game.Title.v1.0-GROUP"), Times.Once());
        }

        [Test]
        public void should_use_game_parameter_when_provided_for_folder_import()
        {
            Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            Mocker.GetMock<IParsingService>()
                  .Verify(v => v.GetGame(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.IsAny<List<string>>(), _game, It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), true), Times.Once());
        }

        [Test]
        public void should_use_auto_importmode_as_move_when_downloadclient_can_move()
        {
            GivenGameParsedFromFolder();
            GivenSuccessfulFolderImport();

            _downloadClientItem.CanMoveFiles = true;

            Mocker.GetMock<IDiskScanService>()
                  .Setup(s => s.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(System.Array.Empty<string>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                  .Returns(System.Array.Empty<string>());

            Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Once());
        }

        [Test]
        public void should_use_auto_importmode_as_copy_when_downloadclient_cannot_move()
        {
            GivenGameParsedFromFolder();
            GivenSuccessfulFolderImport();

            _downloadClientItem.CanMoveFiles = false;

            Subject.ProcessPath(_folderPath, ImportMode.Auto, _game, _downloadClientItem);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_process_root_folder_subfolders_and_files()
        {
            var rootPath = @"C:\downloads\".AsOsAgnostic();
            var subFolder = @"C:\downloads\subfolder".AsOsAgnostic();
            var videoFile = @"C:\downloads\standalone.iso".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(rootPath))
                  .Returns(new[] { subFolder });

            Mocker.GetMock<IDiskScanService>()
                  .Setup(s => s.GetVideoFiles(rootPath, false))
                  .Returns(new[] { videoFile });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(videoFile))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(videoFile))
                  .Returns(true);

            Subject.ProcessRootFolder(new DirectoryInfo(rootPath));

            Mocker.GetMock<IParsingService>()
                  .Verify(v => v.GetGame(It.IsAny<string>()), Times.AtLeast(1));
        }
    }
}
