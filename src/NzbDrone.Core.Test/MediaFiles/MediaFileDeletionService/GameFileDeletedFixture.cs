using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaFileDeletionService
{
    [TestFixture]
    public class GameFileDeletedFixture : CoreTest<Core.MediaFiles.MediaFileDeletionService>
    {
        private Game _game;
        private GameFile _gameFile;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                   .With(s => s.Path = @"C:\Test\Games\Game Title".AsOsAgnostic())
                                   .Build();

            _gameFile = Builder<GameFile>.CreateNew()
                                           .With(f => f.Game = _game)
                                           .With(f => f.Path = @"C:\Test\Games\Game Title\Game Title.mkv".AsOsAgnostic())
                                           .Build();

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DeleteEmptyFolders)
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderEmpty(_game.Path))
                  .Returns(true);
        }

        [Test]
        public void should_not_delete_game_folder_if_game_file_was_missing_from_disk()
        {
            Subject.Handle(new GameFileDeletedEvent(_gameFile, DeleteMediaFileReason.MissingFromDisk));

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.RemoveEmptySubfolders(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_delete_empty_game_folder_if_game_file_was_deleted_manually()
        {
            Subject.Handle(new GameFileDeletedEvent(_gameFile, DeleteMediaFileReason.Manual));

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(_game.Path, true), Times.Once());
        }

        [Test]
        public void should_not_delete_game_folder_if_delete_empty_folders_is_disabled()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DeleteEmptyFolders)
                  .Returns(false);

            Subject.Handle(new GameFileDeletedEvent(_gameFile, DeleteMediaFileReason.Manual));

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.DeleteFolder(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }
    }
}
