using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaInfo
{
    [TestFixture]
    public class UpdateMediaInfoServiceFixture : CoreTest<UpdateMediaInfoService>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 1,
                Path = @"C:\game".AsOsAgnostic()
            };

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.EnableMediaInfo)
                  .Returns(true);
        }

        private void GivenFileExists()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);
        }

        private void GivenSuccessfulScan()
        {
            Mocker.GetMock<IVideoFileInfoReader>()
                  .Setup(v => v.GetMediaInfo(It.IsAny<string>()))
                  .Returns(new MediaInfoModel());
        }

        private void GivenFailedScan(string path)
        {
            Mocker.GetMock<IVideoFileInfoReader>()
                  .Setup(v => v.GetMediaInfo(path))
                  .Returns((MediaInfoModel)null);
        }

        [Test]
        public void should_skip_up_to_date_media_info()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(3)
                .All()
                .With(v => v.Path = null)
                .With(v => v.RelativePath = "media.mkv")
                .TheFirst(1)
                .With(v => v.MediaInfo = new MediaInfoModel { SchemaRevision = VideoFileInfoReader.CURRENT_MEDIA_INFO_SCHEMA_REVISION })
                .BuildList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(v => v.GetFilesByGame(1))
                  .Returns(gameFiles);

            GivenFileExists();
            GivenSuccessfulScan();

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IVideoFileInfoReader>()
                  .Verify(v => v.GetMediaInfo(Path.Combine(_game.Path, "media.mkv")), Times.Exactly(2));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Exactly(2));
        }

        [Test]
        public void should_skip_not_yet_date_media_info()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(3)
                .All()
                .With(v => v.Path = null)
                .With(v => v.RelativePath = "media.mkv")
                .TheFirst(1)
                .With(v => v.MediaInfo = new MediaInfoModel { SchemaRevision = VideoFileInfoReader.MINIMUM_MEDIA_INFO_SCHEMA_REVISION })
                .BuildList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(v => v.GetFilesByGame(1))
                  .Returns(gameFiles);

            GivenFileExists();
            GivenSuccessfulScan();

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IVideoFileInfoReader>()
                  .Verify(v => v.GetMediaInfo(Path.Combine(_game.Path, "media.mkv")), Times.Exactly(2));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Exactly(2));
        }

        [Test]
        public void should_update_outdated_media_info()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(3)
                .All()
                .With(v => v.Path = null)
                .With(v => v.RelativePath = "media.mkv")
                .TheFirst(1)
                .With(v => v.MediaInfo = new MediaInfoModel())
                .BuildList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(v => v.GetFilesByGame(1))
                  .Returns(gameFiles);

            GivenFileExists();
            GivenSuccessfulScan();

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IVideoFileInfoReader>()
                  .Verify(v => v.GetMediaInfo(Path.Combine(_game.Path, "media.mkv")), Times.Exactly(3));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Exactly(3));
        }

        [Test]
        public void should_ignore_missing_files()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(2)
                   .All()
                   .With(v => v.RelativePath = "media.mkv")
                   .BuildList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(v => v.GetFilesByGame(1))
                  .Returns(gameFiles);

            GivenSuccessfulScan();

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IVideoFileInfoReader>()
                  .Verify(v => v.GetMediaInfo("media.mkv"), Times.Never());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Never());
        }

        [Test]
        public void should_continue_after_failure()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(2)
                   .All()
                   .With(v => v.Path = null)
                   .With(v => v.RelativePath = "media.mkv")
                   .TheFirst(1)
                   .With(v => v.RelativePath = "media2.mkv")
                   .BuildList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(v => v.GetFilesByGame(1))
                  .Returns(gameFiles);

            GivenFileExists();
            GivenSuccessfulScan();
            GivenFailedScan(Path.Combine(_game.Path, "media2.mkv"));

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IVideoFileInfoReader>()
                  .Verify(v => v.GetMediaInfo(Path.Combine(_game.Path, "media.mkv")), Times.Exactly(1));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Exactly(1));
        }

        [Test]
        public void should_not_update_files_if_media_info_disabled()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(2)
                .All()
                .With(v => v.RelativePath = "media.mkv")
                .TheFirst(1)
                .With(v => v.RelativePath = "media2.mkv")
                .BuildList();

            Mocker.GetMock<IMediaFileService>()
                .Setup(v => v.GetFilesByGame(1))
                .Returns(gameFiles);

            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.EnableMediaInfo)
                .Returns(false);

            GivenFileExists();
            GivenSuccessfulScan();

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IVideoFileInfoReader>()
                .Verify(v => v.GetMediaInfo(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IMediaFileService>()
                .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Never());
        }

        [Test]
        public void should_not_update_if_media_info_disabled()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(v => v.RelativePath = "media.mkv")
                .Build();

            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.EnableMediaInfo)
                .Returns(false);

            GivenFileExists();
            GivenSuccessfulScan();

            Subject.Update(gameFile, _game);

            Mocker.GetMock<IVideoFileInfoReader>()
                .Verify(v => v.GetMediaInfo(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IMediaFileService>()
                .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Never());
        }

        [Test]
        public void should_update_media_info()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(v => v.Path = null)
                .With(v => v.RelativePath = "media.mkv")
                .With(e => e.MediaInfo = new MediaInfoModel { SchemaRevision = 3 })
                .Build();

            GivenFileExists();
            GivenSuccessfulScan();

            Subject.Update(gameFile, _game);

            Mocker.GetMock<IVideoFileInfoReader>()
                .Verify(v => v.GetMediaInfo(Path.Combine(_game.Path, "media.mkv")), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                .Verify(v => v.Update(gameFile), Times.Once());
        }

        [Test]
        public void should_not_update_media_info_if_new_info_is_null()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(v => v.RelativePath = "media.mkv")
                .With(e => e.MediaInfo = new MediaInfoModel { SchemaRevision = 3 })
                .Build();

            GivenFileExists();
            GivenFailedScan(Path.Combine(_game.Path, "media.mkv"));

            Subject.Update(gameFile, _game);

            gameFile.MediaInfo.Should().NotBeNull();
        }

        [Test]
        public void should_not_save_game_file_if_new_info_is_null()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(v => v.RelativePath = "media.mkv")
                .With(e => e.MediaInfo = new MediaInfoModel { SchemaRevision = 3 })
                .Build();

            GivenFileExists();
            GivenFailedScan(Path.Combine(_game.Path, "media.mkv"));

            Subject.Update(gameFile, _game);

            Mocker.GetMock<IMediaFileService>()
                .Verify(v => v.Update(gameFile), Times.Never());
        }

        [Test]
        public void should_not_update_media_info_if_file_does_not_support_media_info()
        {
            var path = Path.Combine(_game.Path, "media.iso");

            var gameFile = Builder<GameFile>.CreateNew()
                .With(v => v.Path = path)
                .Build();

            GivenFileExists();
            GivenFailedScan(path);

            Subject.Update(gameFile, _game);

            Mocker.GetMock<IVideoFileInfoReader>()
                .Verify(v => v.GetMediaInfo(path), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                .Verify(v => v.Update(gameFile), Times.Never());
        }
    }
}
