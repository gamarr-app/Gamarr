using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class GameFileMovingServiceFixture : CoreTest<GameFileMovingService>
    {
        private Game _game;
        private GameFile _gameFile;
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 1,
                Path = @"C:\Games\TestGame".AsOsAgnostic()
            };

            _gameFile = new GameFile
            {
                Id = 1,
                RelativePath = "game.exe",
                Path = @"C:\Downloads\TestGame-CODEX\game.exe".AsOsAgnostic()
            };

            _localGame = new LocalGame
            {
                Game = _game,
                Path = @"C:\Downloads\TestGame-CODEX".AsOsAgnostic()
            };

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildFileName(It.IsAny<Game>(), It.IsAny<GameFile>(), It.IsAny<NamingConfig>(), It.IsAny<List<CustomFormat>>()))
                  .Returns("TestGame");

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildFilePath(It.IsAny<Game>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(@"C:\Games\TestGame\TestGame.exe".AsOsAgnostic());
        }

        [Test]
        public void should_move_folder_to_game_path()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_localGame.Path))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_game.Path))
                  .Returns(true);

            Subject.MoveGameFile(_gameFile, _localGame);

            var expectedDest = Path.Combine(_game.Path, "TestGame-CODEX");

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(s => s.TransferFolder(
                      _localGame.Path,
                      expectedDest,
                      TransferMode.Move), Times.Once());
        }

        [Test]
        public void should_copy_folder_to_game_path()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_localGame.Path))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_game.Path))
                  .Returns(true);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CopyUsingHardlinks)
                  .Returns(false);

            Subject.CopyGameFile(_gameFile, _localGame);

            var expectedDest = Path.Combine(_game.Path, "TestGame-CODEX");

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(s => s.TransferFolder(
                      _localGame.Path,
                      expectedDest,
                      TransferMode.Copy), Times.Once());
        }

        [Test]
        public void should_throw_when_source_folder_does_not_exist()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_localGame.Path))
                  .Returns(true);

            // Return true for the initial FolderExists check in MoveGameFile,
            // but return false when TransferGamePath checks
            var callCount = 0;
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_localGame.Path))
                  .Returns(() =>
                  {
                      callCount++;
                      return callCount <= 1; // true first time (in MoveGameFile), false second time (in TransferGamePath)
                  });

            Assert.Throws<DirectoryNotFoundException>(() => Subject.MoveGameFile(_gameFile, _localGame));
        }

        [Test]
        public void should_create_parent_directory_when_missing()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_localGame.Path))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_game.Path))
                  .Returns(false);

            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.CreateFolder(_game.Path), Times.Once());
        }

        [Test]
        public void should_use_file_transfer_when_source_is_file()
        {
            var filePath = @"C:\Downloads\TestGame.exe".AsOsAgnostic();
            _localGame.Path = filePath;

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(filePath))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_game.Path))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns(true);

            var rootFolder = @"C:\Games".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), It.IsAny<List<RootFolder>>()))
                  .Returns(rootFolder);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(rootFolder))
                  .Returns(true);

            Mocker.GetMock<IDiskTransferService>()
                  .Setup(s => s.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>(), It.IsAny<bool>()))
                  .Returns(TransferMode.Move);

            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(s => s.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>()), Times.Never());
        }

        [Test]
        public void should_set_relative_path_after_folder_move()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_localGame.Path))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_game.Path))
                  .Returns(true);

            var result = Subject.MoveGameFile(_gameFile, _localGame);

            result.RelativePath.Should().NotBeNullOrEmpty();
        }
    }
}
