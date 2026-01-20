using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ReleaseInfoFixture
    {
        [Test]
        public void constructor_should_initialize_languages()
        {
            var release = new ReleaseInfo();

            release.Languages.Should().NotBeNull();
            release.Languages.Should().BeEmpty();
        }

        [Test]
        public void Age_should_return_days_since_publish_date()
        {
            var release = new ReleaseInfo
            {
                PublishDate = DateTime.UtcNow.AddDays(-5)
            };

            release.Age.Should().BeInRange(4, 6);
        }

        [Test]
        public void Age_should_return_zero_for_current_date()
        {
            var release = new ReleaseInfo
            {
                PublishDate = DateTime.UtcNow
            };

            release.Age.Should().Be(0);
        }

        [Test]
        public void AgeHours_should_return_hours_since_publish_date()
        {
            var release = new ReleaseInfo
            {
                PublishDate = DateTime.UtcNow.AddHours(-24)
            };

            release.AgeHours.Should().BeInRange(23, 25);
        }

        [Test]
        public void AgeMinutes_should_return_minutes_since_publish_date()
        {
            var release = new ReleaseInfo
            {
                PublishDate = DateTime.UtcNow.AddMinutes(-60)
            };

            release.AgeMinutes.Should().BeInRange(59, 61);
        }

        [Test]
        public void ToString_should_contain_publish_date_title_and_size()
        {
            var release = new ReleaseInfo
            {
                Title = "Test Release",
                Size = 1024,
                PublishDate = new DateTime(2023, 1, 1)
            };

            var result = release.ToString();

            result.Should().Contain("Test Release");
            result.Should().Contain("1024");
            result.Should().Contain("2023");
        }

        [Test]
        public void ToString_with_L_format_should_return_long_format()
        {
            var release = new ReleaseInfo
            {
                Guid = "test-guid",
                Title = "Test Release",
                Size = 1024,
                InfoUrl = "http://info.url",
                DownloadUrl = "http://download.url",
                Indexer = "TestIndexer",
                CommentUrl = "http://comment.url",
                DownloadProtocol = DownloadProtocol.Torrent,
                SteamAppId = 12345,
                IgdbId = 67890,
                PublishDate = new DateTime(2023, 1, 1)
            };

            var result = release.ToString("L");

            result.Should().Contain("Guid: test-guid");
            result.Should().Contain("Title: Test Release");
            result.Should().Contain("Size:");
            result.Should().Contain("InfoUrl: http://info.url");
            result.Should().Contain("DownloadUrl: http://download.url");
            result.Should().Contain("Indexer: TestIndexer");
            result.Should().Contain("CommentUrl: http://comment.url");
            result.Should().Contain("SteamAppId:");
            result.Should().Contain("IgdbId:");
        }

        [Test]
        public void ToString_with_unknown_format_should_return_default()
        {
            var release = new ReleaseInfo
            {
                Title = "Test Release",
                Size = 1024,
                PublishDate = new DateTime(2023, 1, 1)
            };

            var result = release.ToString("X");

            result.Should().Be(release.ToString());
        }

        [Test]
        public void ToString_with_lowercase_l_format_should_work()
        {
            var release = new ReleaseInfo
            {
                Title = "Test Release"
            };

            var result = release.ToString("l");

            result.Should().Contain("Title:");
        }

        [Test]
        public void should_be_able_to_set_all_properties()
        {
            var release = new ReleaseInfo
            {
                Guid = "guid-123",
                Title = "Test Title",
                Size = 2048,
                DownloadUrl = "http://download.com",
                InfoUrl = "http://info.com",
                CommentUrl = "http://comment.com",
                IndexerId = 1,
                Indexer = "TestIndexer",
                IndexerPriority = 10,
                DownloadProtocol = DownloadProtocol.Usenet,
                SteamAppId = 100,
                IgdbId = 200,
                PublishDate = DateTime.Now,
                Origin = "Test Origin",
                Source = "Test Source",
                Container = "Test Container",
                Codec = "Test Codec",
                Resolution = "1080p"
            };

            release.Guid.Should().Be("guid-123");
            release.Title.Should().Be("Test Title");
            release.Size.Should().Be(2048);
            release.DownloadUrl.Should().Be("http://download.com");
            release.InfoUrl.Should().Be("http://info.com");
            release.CommentUrl.Should().Be("http://comment.com");
            release.IndexerId.Should().Be(1);
            release.Indexer.Should().Be("TestIndexer");
            release.IndexerPriority.Should().Be(10);
            release.DownloadProtocol.Should().Be(DownloadProtocol.Usenet);
            release.SteamAppId.Should().Be(100);
            release.IgdbId.Should().Be(200);
            release.Origin.Should().Be("Test Origin");
            release.Source.Should().Be("Test Source");
            release.Container.Should().Be("Test Container");
            release.Codec.Should().Be("Test Codec");
            release.Resolution.Should().Be("1080p");
        }

        [Test]
        public void should_be_able_to_set_languages()
        {
            var release = new ReleaseInfo();
            release.Languages.Add(Language.English);
            release.Languages.Add(Language.French);

            release.Languages.Should().HaveCount(2);
            release.Languages.Should().Contain(Language.English);
            release.Languages.Should().Contain(Language.French);
        }

        [Test]
        public void IndexerFlags_should_be_settable()
        {
            var release = new ReleaseInfo
            {
                IndexerFlags = IndexerFlags.G_Internal | IndexerFlags.G_Scene
            };

            release.IndexerFlags.Should().HaveFlag(IndexerFlags.G_Internal);
            release.IndexerFlags.Should().HaveFlag(IndexerFlags.G_Scene);
        }
    }
}
