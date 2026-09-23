using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaCoverTests
{
    [TestFixture]
    public class MediaCoverServiceFixture : CoreTest<MediaCoverService>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IAppFolderInfo>(new AppFolderInfo(Mocker.Resolve<IStartupContext>()));

            _game = Builder<Game>.CreateNew()
                .With(v => v.Id = 2)
                .With(v => v.Added = DateTime.UtcNow)
                .With(v => v.GameMetadata.Value.Images = new List<MediaCover.MediaCover> { new MediaCover.MediaCover(MediaCoverTypes.Poster, "") })
                .Build();

            Mocker.GetMock<IGameService>().Setup(m => m.GetGame(It.Is<int>(id => id == _game.Id))).Returns(_game);
        }

        [Test]
        public void should_convert_cover_urls_to_local_with_remote_url_hash()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner, RemoteUrl = "https://example.com/banner.jpg" }
                };

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ConvertToLocalUrls(12, covers, DateTime.UtcNow);

            covers.Single().Url.Should().StartWith("/MediaCover/12/banner.jpg?h=");
            covers.Single().Url.Split("?h=")[1].Should().HaveLength(20);
        }

        [Test]
        public void should_convert_cover_urls_to_local_without_hash_if_cover_has_not_been_downloaded()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner, RemoteUrl = "https://example.com/banner.jpg" }
                };

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.ConvertToLocalUrls(12, covers, DateTime.UtcNow);

            covers.Single().Url.Should().Be("/MediaCover/12/banner.jpg");
        }

        [Test]
        public void should_only_check_if_cover_exists_on_disk_once()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner, RemoteUrl = "https://example.com/banner.jpg" }
                };

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ConvertToLocalUrls(12, covers, DateTime.UtcNow);
            Subject.ConvertToLocalUrls(12, covers, DateTime.UtcNow);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.FileExists(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_check_if_cover_exists_for_game_added_more_than_a_day_ago()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner, RemoteUrl = "https://example.com/banner.jpg" }
                };

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.ConvertToLocalUrls(12, covers, DateTime.UtcNow.AddDays(-2));

            covers.Single().Url.Should().StartWith("/MediaCover/12/banner.jpg?h=");

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.FileExists(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_add_hash_to_cover_url_once_cover_exists()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Poster, RemoteUrl = "https://example.com/poster.jpg" }
                };

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.ConvertToLocalUrls(_game.Id, covers, _game.Added);

            covers.Single().Url.Should().Be($"/MediaCover/{_game.Id}/poster.jpg");

            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Subject.HandleAsync(new GameUpdatedEvent(_game));
            Subject.ConvertToLocalUrls(_game.Id, covers, _game.Added);

            covers.Single().Url.Should().StartWith($"/MediaCover/{_game.Id}/poster.jpg?h=");
        }

        [Test]
        public void should_check_each_screenshot_separately()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Screenshot, RemoteUrl = "https://example.com/screenshot1.jpg" },
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Screenshot, RemoteUrl = "https://example.com/screenshot2.jpg" }
                };

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns<string>(path => path.EndsWith("screenshot.jpg"));

            Subject.ConvertToLocalUrls(12, covers, DateTime.UtcNow);

            covers[0].Url.Should().StartWith("/MediaCover/12/screenshot.jpg?h=");
            covers[1].Url.Should().Be("/MediaCover/12/screenshot2.jpg");
        }

        [Test]
        public void should_convert_media_urls_to_local_without_hash_when_no_remote_url()
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner }
                };

            Subject.ConvertToLocalUrls(12, covers, DateTime.UtcNow);

            covers.Single().Url.Should().Be("/MediaCover/12/banner.jpg");
        }

        [Test]
        public void should_resize_covers_if_main_downloaded()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.HandleAsync(new GameUpdatedEvent(_game));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_resize_covers_if_missing()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.HandleAsync(new GameUpdatedEvent(_game));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_not_resize_covers_if_exists()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetFileSize(It.IsAny<string>()))
                  .Returns(1000);

            Subject.HandleAsync(new GameUpdatedEvent(_game));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_resize_covers_if_existing_is_empty()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetFileSize(It.IsAny<string>()))
                  .Returns(0);

            Subject.HandleAsync(new GameUpdatedEvent(_game));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_log_error_if_resize_failed()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IImageResizer>()
                  .Setup(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                  .Throws<ApplicationException>();

            Subject.HandleAsync(new GameUpdatedEvent(_game));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }
    }
}
