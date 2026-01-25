using System;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameFileMovingServiceTests
{
    [TestFixture]
    public class MoveGameFileFixture : CoreTest<GameFileMovingService>
    {
        private Game _game;
        private GameFile _gameFile;
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\Games\Game".AsOsAgnostic())
                                     .Build();

            _gameFile = Builder<GameFile>.CreateNew()
                                               .With(f => f.Path = null)
                                               .With(f => f.RelativePath = @"File.avi")
                                               .Build();

            _localGame = Builder<LocalGame>.CreateNew()
                                                 .With(l => l.Game = _game)
                                                 .Build();

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildFileName(It.IsAny<Game>(), It.IsAny<GameFile>(), null, null))
                  .Returns("File Name");

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildFilePath(It.IsAny<Game>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(@"C:\Test\Games\Game\File Name.avi".AsOsAgnostic());

            var rootFolder = @"C:\Test\Games\".AsOsAgnostic();

            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), null))
                .Returns(rootFolder);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(rootFolder))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns(true);
        }

        [Test]
        public void should_catch_UnauthorizedAccessException_during_folder_inheritance()
        {
            WindowsOnly();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.InheritFolderPermissions(It.IsAny<string>()))
                  .Throws<UnauthorizedAccessException>();

            Subject.MoveGameFile(_gameFile, _localGame);
        }

        [Test]
        public void should_catch_InvalidOperationException_during_folder_inheritance()
        {
            WindowsOnly();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.InheritFolderPermissions(It.IsAny<string>()))
                  .Throws<InvalidOperationException>();

            Subject.MoveGameFile(_gameFile, _localGame);
        }

        [Test]
        public void should_notify_on_game_folder_creation()
        {
            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<GameFolderCreatedEvent>(It.Is<GameFolderCreatedEvent>(p =>
                      p.GameFolder.IsNotNullOrWhiteSpace())), Times.Once());
        }

        [Test]
        public void should_not_notify_if_game_folder_already_exists()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_game.Path))
                  .Returns(true);

            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<GameFolderCreatedEvent>(It.Is<GameFolderCreatedEvent>(p =>
                      p.GameFolder.IsNotNullOrWhiteSpace())), Times.Never());
        }
    }

    [TestFixture]
    public class MoveFolderGameFileFixture : CoreTest<GameFileMovingService>
    {
        private Game _game;
        private GameFile _gameFile;
        private LocalGame _localGame;
        private string _sourceFolderPath;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                .With(s => s.Path = @"C:\Test\Games\Game".AsOsAgnostic())
                .Build();

            _gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.Path = null)
                .With(f => f.RelativePath = string.Empty)
                .Build();

            _sourceFolderPath = @"C:\Downloads\Game-RUNE".AsOsAgnostic();

            _localGame = Builder<LocalGame>.CreateNew()
                .With(l => l.Game = _game)
                .With(l => l.Path = _sourceFolderPath)
                .Build();

            var rootFolder = @"C:\Test\Games\".AsOsAgnostic();

            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), null))
                .Returns(rootFolder);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(rootFolder))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_game.Path))
                .Returns(true);

            // Source is a folder
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_sourceFolderPath))
                .Returns(true);
        }

        [Test]
        public void should_transfer_folder_to_game_path()
        {
            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IDiskTransferService>()
                .Verify(v => v.TransferFolder(_sourceFolderPath, _game.Path, TransferMode.Move), Times.Once());
        }

        [Test]
        public void should_set_relative_path_to_empty_for_folder_move()
        {
            var result = Subject.MoveGameFile(_gameFile, _localGame);

            result.RelativePath.Should().BeEmpty();
        }

        [Test]
        public void should_create_game_folder_if_not_exists_before_folder_transfer()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_game.Path))
                .Returns(false);

            Subject.MoveGameFile(_gameFile, _localGame);

            Mocker.GetMock<IDiskProvider>()
                .Verify(v => v.CreateFolder(_game.Path), Times.Once());
        }

        [Test]
        public void should_throw_if_source_folder_does_not_exist()
        {
            // Return true on first check (in MoveGameFile), false on second (in TransferGamePath)
            var callCount = 0;
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_sourceFolderPath))
                .Returns(() =>
                {
                    callCount++;
                    return callCount <= 1;
                });

            Assert.Throws<System.IO.DirectoryNotFoundException>(() => Subject.MoveGameFile(_gameFile, _localGame));
        }
    }
}
