using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Archives;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameFileMovingServiceTests
{
    [TestFixture]
    public class ExtractArchivedGameFileFixture : CoreTest<GameFileMovingService>
    {
        private Game _game;
        private GameFile _gameFile;
        private LocalGame _localGame;
        private string _archivePath;
        private string _destinationPath;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 1,
                Path = @"C:\Games\Kirby's Return to Dream Land Deluxe".AsOsAgnostic()
            };

            _archivePath = @"C:\Downloads\Kirby (BlueRoms)\Kirby.7z".AsOsAgnostic();
            _destinationPath = @"C:\Games\Kirby's Return to Dream Land Deluxe\Kirby [Base].xci".AsOsAgnostic();

            _gameFile = new GameFile
            {
                Id = 1,
                RelativePath = "Kirby.7z",
                Path = _archivePath
            };

            _localGame = new LocalGame
            {
                Game = _game,
                Path = _archivePath,
                Size = 7985954816,
                ArchiveInspection = new GameArchiveInspection
                {
                    ArchivePath = _archivePath,
                    EntryName = "Kirby's Return to Dream Land Deluxe [0100227010F46000][v0].xci",
                    EntrySize = 7985954816
                }
            };

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildFileName(It.IsAny<Game>(), It.IsAny<GameFile>(), It.IsAny<NamingConfig>(), It.IsAny<List<CustomFormat>>()))
                  .Returns("Kirby [Base]");

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildFilePath(It.IsAny<Game>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(_destinationPath);

            var rootFolder = @"C:\Games\".AsOsAgnostic();

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), It.IsAny<List<RootFolder>>()))
                  .Returns(rootFolder);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);

            // The archive is a file, so the folder branch must not be taken.
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_archivePath))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns(true);
        }

        [Test]
        public void should_extract_the_entry_instead_of_transferring_the_archive()
        {
            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IGameArchiveService>()
                  .Verify(s => s.ExtractEntry(_localGame.ArchiveInspection, _destinationPath), Times.Once());

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(s => s.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>(), It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(s => s.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>()), Times.Never());
        }

        [Test]
        public void should_name_the_destination_with_the_inner_entry_extension()
        {
            // XCI bytes named "....7z" would be just as invisible to a console
            // library scanner as the archive was.
            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IBuildFileNames>()
                  .Verify(s => s.BuildFilePath(_game, "Kirby [Base]", ".xci"), Times.Once());
        }

        [Test]
        public void should_bypass_the_custom_import_script()
        {
            // A script handed the archive path cannot do the right thing with it.
            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IImportScript>()
                  .Verify(s => s.TryImport(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LocalGame>(), It.IsAny<GameFile>(), It.IsAny<TransferMode>()), Times.Never());
        }

        [Test]
        public void should_not_hardlink_an_extracted_game_file()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CopyUsingHardlinks)
                  .Returns(true);

            Subject.CopyGameFile(_gameFile, _localGame);

            Mocker.GetMock<IGameArchiveService>()
                  .Verify(s => s.ExtractEntry(_localGame.ArchiveInspection, _destinationPath), Times.Once());

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(s => s.TransferFile(It.IsAny<string>(), It.IsAny<string>(), TransferMode.HardLinkOrCopy, It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_still_hardlink_a_plain_game_file()
        {
            _localGame.ArchiveInspection = null;
            _localGame.Path = @"C:\Downloads\Kirby (BlueRoms)\Kirby.xci".AsOsAgnostic();
            _gameFile.Path = _localGame.Path;

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_localGame.Path))
                  .Returns(false);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CopyUsingHardlinks)
                  .Returns(true);

            Mocker.GetMock<IDiskTransferService>()
                  .Setup(s => s.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>(), It.IsAny<bool>()))
                  .Returns(TransferMode.HardLink);

            // Default(ScriptImportDecision) is MoveComplete, which would skip the
            // transfer entirely; no script is configured in this scenario.
            Mocker.GetMock<IImportScript>()
                  .Setup(s => s.TryImport(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LocalGame>(), It.IsAny<GameFile>(), It.IsAny<TransferMode>()))
                  .Returns(ScriptImportDecision.DeferMove);

            Subject.CopyGameFile(_gameFile, _localGame);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(s => s.TransferFile(_localGame.Path, _destinationPath, TransferMode.HardLinkOrCopy, It.IsAny<bool>()), Times.Once());

            Mocker.GetMock<IGameArchiveService>()
                  .Verify(s => s.ExtractEntry(It.IsAny<GameArchiveInspection>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_record_the_relative_path_of_the_extracted_file()
        {
            var result = Subject.MoveGameFile(_gameFile, _localGame);

            result.RelativePath.Should().Be("Kirby [Base].xci");
        }
    }
}
