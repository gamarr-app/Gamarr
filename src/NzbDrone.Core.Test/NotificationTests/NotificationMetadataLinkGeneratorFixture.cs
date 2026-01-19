using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class NotificationMetadataLinkGeneratorFixture : CoreTest
    {
        private Game _game;
        private GameMetadata _gameMetadata;

        [SetUp]
        public void Setup()
        {
            _gameMetadata = new GameMetadata
            {
                IgdbId = 1942,
                Title = "The Witcher 3: Wild Hunt",
                SteamAppId = 292030
            };

            _game = new Game
            {
                Id = 1,
                IgdbId = 1942,
                Title = "The Witcher 3: Wild Hunt",
                GameMetadata = new LazyLoaded<GameMetadata>(_gameMetadata)
            };
        }

        [Test]
        public void should_return_empty_list_when_game_is_null()
        {
            var result = NotificationMetadataLinkGenerator.GenerateLinks(null, new List<int> { 0, 1, 2 });

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void should_generate_igdb_link()
        {
            var links = new List<int> { (int)MetadataLinkType.Igdb };

            var result = NotificationMetadataLinkGenerator.GenerateLinks(_game, links);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(MetadataLinkType.Igdb);
            result[0].Label.Should().Be("IGDB");
            result[0].Link.Should().Contain("igdb.com/games/1942");
        }

        [Test]
        public void should_generate_steam_link_when_steam_app_id_exists()
        {
            var links = new List<int> { (int)MetadataLinkType.Steam };

            var result = NotificationMetadataLinkGenerator.GenerateLinks(_game, links);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(MetadataLinkType.Steam);
            result[0].Label.Should().Be("Steam");
            result[0].Link.Should().Contain("store.steampowered.com/app/292030");
        }

        [Test]
        public void should_not_generate_steam_link_when_steam_app_id_is_zero()
        {
            _gameMetadata.SteamAppId = 0;
            var links = new List<int> { (int)MetadataLinkType.Steam };

            var result = NotificationMetadataLinkGenerator.GenerateLinks(_game, links);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void should_generate_multiple_links_when_ids_exist()
        {
            var links = new List<int>
            {
                (int)MetadataLinkType.Igdb,
                (int)MetadataLinkType.Steam
            };

            var result = NotificationMetadataLinkGenerator.GenerateLinks(_game, links);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Select(l => l.Type).Should().Contain(MetadataLinkType.Igdb);
            result.Select(l => l.Type).Should().Contain(MetadataLinkType.Steam);
        }

        [Test]
        public void should_not_generate_igdb_link_when_igdb_id_is_zero()
        {
            _game.IgdbId = 0;
            var links = new List<int> { (int)MetadataLinkType.Igdb };

            var result = NotificationMetadataLinkGenerator.GenerateLinks(_game, links);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void should_return_empty_list_for_empty_metadata_links()
        {
            var result = NotificationMetadataLinkGenerator.GenerateLinks(_game, new List<int>());

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}
