using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class MediaFileTableCleanupServiceFixture : CoreTest<MediaFileTableCleanupService>
    {
        private const string DELETED_PATH = "ANY FILE WITH THIS PATH IS CONSIDERED DELETED!";
        private Game _game;

        [SetUp]
        public void SetUp()
        {
            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\TV\Series".AsOsAgnostic())
                                     .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(e => e.FileExists(It.Is<string>(c => !c.Contains(DELETED_PATH))))
                  .Returns(true);
        }

        private void GivenGameFiles(IEnumerable<GameFile> gameFiles)
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFilesByGame(It.IsAny<int>()))
                  .Returns(gameFiles.ToList());
        }

        private void GivenFilesAreNotAttachedToEpisode()
        {
            _game.GameFileId = 0;
        }

        private List<string> FilesOnDisk(IEnumerable<GameFile> gameFiles)
        {
            return gameFiles.Select(e => Path.Combine(_game.Path, e.RelativePath)).ToList();
        }

        [Test]
        public void should_skip_files_that_exist_in_disk()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(10)
                .Build();

            GivenGameFiles(gameFiles);

            Subject.Clean(_game, FilesOnDisk(gameFiles));

            Mocker.GetMock<IGameService>().Verify(c => c.UpdateGame(It.IsAny<Game>()), Times.Never());
        }

        [Test]
        public void should_delete_non_existent_files()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(10)
                .Random(2)
                .With(c => c.RelativePath = DELETED_PATH)
                .Build();

            GivenGameFiles(gameFiles);

            Subject.Clean(_game, FilesOnDisk(gameFiles.Where(e => e.RelativePath != DELETED_PATH)));

            Mocker.GetMock<IMediaFileService>().Verify(c => c.Delete(It.Is<GameFile>(e => e.RelativePath == DELETED_PATH), DeleteMediaFileReason.MissingFromDisk), Times.Exactly(2));
        }

        [Test]
        [Ignore("idc")]
        public void should_delete_files_that_dont_belong_to_any_episodes()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.RelativePath = "ExistingPath")
                                .Build();

            GivenGameFiles(gameFiles);
            GivenFilesAreNotAttachedToEpisode();

            Subject.Clean(_game, FilesOnDisk(gameFiles));

            Mocker.GetMock<IMediaFileService>().Verify(c => c.Delete(It.IsAny<GameFile>(), DeleteMediaFileReason.NoLinkedEpisodes), Times.Exactly(10));
        }

        [Test]
        [Ignore("Idc")]
        public void should_unlink_episode_when_episodeFile_does_not_exist()
        {
            GivenGameFiles(new List<GameFile>());

            Subject.Clean(_game, new List<string>());

            Mocker.GetMock<IGameService>().Verify(c => c.UpdateGame(It.Is<Game>(e => e.GameFileId == 0)), Times.Exactly(10));
        }

        [Test]
        public void should_not_update_episode_when_episodeFile_exists()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.RelativePath = "ExistingPath")
                                .Build();

            GivenGameFiles(gameFiles);

            Subject.Clean(_game, FilesOnDisk(gameFiles));

            Mocker.GetMock<IGameService>().Verify(c => c.UpdateGame(It.IsAny<Game>()), Times.Never());
        }
    }
}
