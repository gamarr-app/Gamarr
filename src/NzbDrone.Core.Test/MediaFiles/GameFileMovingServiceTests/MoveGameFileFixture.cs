using System;
using FizzWare.NBuilder;
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
}
