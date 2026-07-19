using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class UpdateRetentionServiceFixture : CoreTest<UpdateRetentionService>
    {
        private Game _game;
        private List<GameFile> _files;

        [SetUp]
        public void Setup()
        {
            _game = new Game { Id = 3, Title = "Hades", Path = @"C:\Games\Hades".AsOsAgnostic() };

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(_game.Id))
                  .Returns(_game);

            Mocker.GetMock<IConfigService>().SetupGet(s => s.UpdateRetentionCount).Returns(3);
            Mocker.GetMock<IConfigService>().SetupGet(s => s.UpdateRetentionKeepOnePerMajor).Returns(true);

            _files = new List<GameFile>();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(_files);
        }

        private GameFile GivenUpdate(int id, string version, int componentId = 0)
        {
            var file = new GameFile
            {
                Id = id,
                GameId = _game.Id,
                RelativePath = $"Updates/v{version}",
                GameVersion = GameVersion.Parse(version),
                ComponentId = componentId
            };

            _files.Add(file);
            return file;
        }

        private void WhenUpdateAdded()
        {
            Subject.Handle(new GameFileAddedEvent(_files.Last()));
        }

        [Test]
        public void should_keep_all_when_under_retention_count()
        {
            GivenUpdate(1, "1.1");
            GivenUpdate(2, "1.2");
            GivenUpdate(3, "1.3");

            WhenUpdateAdded();

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        [Test]
        public void should_prune_oldest_beyond_retention_count()
        {
            GivenUpdate(1, "1.1");
            GivenUpdate(2, "1.2");
            GivenUpdate(3, "1.3");
            GivenUpdate(4, "1.4", componentId: 44);

            WhenUpdateAdded();

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Delete(It.Is<GameFile>(f => f.Id == 1), DeleteMediaFileReason.Upgrade), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Once());
        }

        [Test]
        public void should_keep_newest_of_each_major_version()
        {
            GivenUpdate(1, "1.9");
            GivenUpdate(2, "2.1");
            GivenUpdate(3, "2.2");
            GivenUpdate(4, "2.3");

            WhenUpdateAdded();

            // v1.9 is beyond the 3 newest but is the newest of major 1
            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        [Test]
        public void should_prune_older_updates_within_same_major()
        {
            GivenUpdate(1, "1.8");
            GivenUpdate(2, "1.9");
            GivenUpdate(3, "2.1");
            GivenUpdate(4, "2.2");
            GivenUpdate(5, "2.3");

            WhenUpdateAdded();

            // Keep: 2.3, 2.2, 2.1 (newest three) + 1.9 (newest of major 1).
            // Prune: 1.8.
            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Delete(It.Is<GameFile>(f => f.Id == 1), DeleteMediaFileReason.Upgrade), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Once());
        }

        [Test]
        public void should_delete_component_slot_and_recycle_folder_for_pruned_update()
        {
            GivenUpdate(1, "1.1", componentId: 11);
            GivenUpdate(2, "1.2");
            GivenUpdate(3, "1.3");
            GivenUpdate(4, "1.4");

            var prunedPath = System.IO.Path.Combine(_game.Path, "Updates/v1.1");

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(prunedPath))
                  .Returns(true);

            WhenUpdateAdded();

            Mocker.GetMock<IRecycleBinProvider>()
                  .Verify(s => s.DeleteFolder(prunedPath), Times.Once());

            Mocker.GetMock<IGameComponentRepository>()
                  .Verify(s => s.Delete(11), Times.Once());
        }

        [Test]
        public void should_keep_everything_when_retention_disabled()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.UpdateRetentionCount).Returns(0);

            GivenUpdate(1, "1.1");
            GivenUpdate(2, "1.2");
            GivenUpdate(3, "1.3");
            GivenUpdate(4, "1.4");

            WhenUpdateAdded();

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        [Test]
        public void should_ignore_non_update_files()
        {
            var baseFile = new GameFile { Id = 9, GameId = _game.Id, RelativePath = string.Empty };

            Subject.Handle(new GameFileAddedEvent(baseFile));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.GetFilesByGame(It.IsAny<int>()), Times.Never());
        }
    }
}
