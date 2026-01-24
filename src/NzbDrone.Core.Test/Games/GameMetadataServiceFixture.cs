using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games
{
    [TestFixture]
    public class GameMetadataServiceFixture : CoreTest<GameMetadataService>
    {
        private GameMetadata _gameMetadata;

        [SetUp]
        public void Setup()
        {
            _gameMetadata = new GameMetadata
            {
                Id = 1,
                IgdbId = 100,
                SteamAppId = 500,
                Title = "Test Game",
                Year = 2023
            };

            Mocker.GetMock<IGameMetadataRepository>()
                  .Setup(s => s.Get(It.IsAny<int>()))
                  .Returns(_gameMetadata);

            Mocker.GetMock<IGameMetadataRepository>()
                  .Setup(s => s.UpsertMany(It.IsAny<List<GameMetadata>>()))
                  .Returns(true);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.ExistsByMetadataId(It.IsAny<int>()))
                  .Returns(false);

            Mocker.GetMock<IImportListGameService>()
                  .Setup(s => s.ExistsByMetadataId(It.IsAny<int>()))
                  .Returns(false);
        }

        [Test]
        public void should_get_metadata_by_id()
        {
            var result = Subject.Get(1);

            result.Should().BeSameAs(_gameMetadata);

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.Get(1), Times.Once());
        }

        [Test]
        public void should_find_metadata_by_igdb_id()
        {
            Mocker.GetMock<IGameMetadataRepository>()
                  .Setup(s => s.FindByIgdbId(100))
                  .Returns(_gameMetadata);

            var result = Subject.FindByIgdbId(100);

            result.Should().BeSameAs(_gameMetadata);

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.FindByIgdbId(100), Times.Once());
        }

        [Test]
        public void should_return_null_when_igdb_id_not_found()
        {
            Mocker.GetMock<IGameMetadataRepository>()
                  .Setup(s => s.FindByIgdbId(999))
                  .Returns((GameMetadata)null);

            var result = Subject.FindByIgdbId(999);

            result.Should().BeNull();
        }

        [Test]
        public void should_upsert_single_game_metadata()
        {
            var newMetadata = new GameMetadata
            {
                IgdbId = 200,
                SteamAppId = 600,
                Title = "New Game"
            };

            var result = Subject.Upsert(newMetadata);

            result.Should().BeTrue();

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.UpsertMany(It.Is<List<GameMetadata>>(l => l.Count == 1 && l[0].Title == "New Game")), Times.Once());
        }

        [Test]
        public void should_upsert_many_game_metadata()
        {
            var metadataList = new List<GameMetadata>
            {
                new GameMetadata { IgdbId = 200, Title = "Game One" },
                new GameMetadata { IgdbId = 201, Title = "Game Two" }
            };

            var result = Subject.UpsertMany(metadataList);

            result.Should().BeTrue();

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.UpsertMany(It.Is<List<GameMetadata>>(l => l.Count == 2)), Times.Once());
        }

        [Test]
        public void should_return_false_when_upsert_has_no_changes()
        {
            Mocker.GetMock<IGameMetadataRepository>()
                  .Setup(s => s.UpsertMany(It.IsAny<List<GameMetadata>>()))
                  .Returns(false);

            var result = Subject.Upsert(_gameMetadata);

            result.Should().BeFalse();
        }

        [Test]
        public void should_delete_metadata_when_not_in_use()
        {
            var metadataToDelete = new List<GameMetadata>
            {
                new GameMetadata { Id = 10, IgdbId = 300, Title = "Unused Game" },
                new GameMetadata { Id = 11, IgdbId = 301, Title = "Also Unused" }
            };

            Subject.DeleteMany(metadataToDelete);

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.Delete(It.Is<GameMetadata>(g => g.Id == 10)), Times.Once());

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.Delete(It.Is<GameMetadata>(g => g.Id == 11)), Times.Once());
        }

        [Test]
        public void should_not_delete_metadata_when_game_exists()
        {
            var metadataToDelete = new List<GameMetadata>
            {
                new GameMetadata { Id = 10, IgdbId = 300, Title = "In Use By Game" }
            };

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.ExistsByMetadataId(10))
                  .Returns(true);

            Subject.DeleteMany(metadataToDelete);

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.Delete(It.IsAny<GameMetadata>()), Times.Never());
        }

        [Test]
        public void should_not_delete_metadata_when_import_list_game_exists()
        {
            var metadataToDelete = new List<GameMetadata>
            {
                new GameMetadata { Id = 10, IgdbId = 300, Title = "In Import List" }
            };

            Mocker.GetMock<IImportListGameService>()
                  .Setup(s => s.ExistsByMetadataId(10))
                  .Returns(true);

            Subject.DeleteMany(metadataToDelete);

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.Delete(It.IsAny<GameMetadata>()), Times.Never());
        }

        [Test]
        public void should_only_delete_unused_metadata_in_mixed_list()
        {
            var metadataToDelete = new List<GameMetadata>
            {
                new GameMetadata { Id = 10, IgdbId = 300, Title = "In Use" },
                new GameMetadata { Id = 11, IgdbId = 301, Title = "Not In Use" }
            };

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.ExistsByMetadataId(10))
                  .Returns(true);

            Subject.DeleteMany(metadataToDelete);

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.Delete(It.Is<GameMetadata>(g => g.Id == 10)), Times.Never());

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.Delete(It.Is<GameMetadata>(g => g.Id == 11)), Times.Once());
        }

        [Test]
        public void should_get_games_with_collections()
        {
            var gamesWithCollections = new List<GameMetadata>
            {
                new GameMetadata { Id = 1, CollectionIgdbId = 50, Title = "Game in Collection" }
            };

            Mocker.GetMock<IGameMetadataRepository>()
                  .Setup(s => s.GetGamesWithCollections())
                  .Returns(gamesWithCollections);

            var result = Subject.GetGamesWithCollections();

            result.Should().HaveCount(1);
            result[0].CollectionIgdbId.Should().Be(50);
        }

        [Test]
        public void should_get_games_by_collection_igdb_id()
        {
            var gamesInCollection = new List<GameMetadata>
            {
                new GameMetadata { Id = 1, CollectionIgdbId = 50, Title = "Game One" },
                new GameMetadata { Id = 2, CollectionIgdbId = 50, Title = "Game Two" }
            };

            Mocker.GetMock<IGameMetadataRepository>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(50))
                  .Returns(gamesInCollection);

            var result = Subject.GetGamesByCollectionIgdbId(50);

            result.Should().HaveCount(2);

            Mocker.GetMock<IGameMetadataRepository>()
                  .Verify(v => v.GetGamesByCollectionIgdbId(50), Times.Once());
        }
    }
}
