using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedGameMetadataFixture : DbTest<CleanupOrphanedGameMetadata, GameMetadata>
    {
        [Test]
        public void should_delete_orphaned_game_metadata_items()
        {
            var metadata = Builder<GameMetadata>.CreateNew().BuildNew();

            Db.Insert(metadata);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_game_metadata_items()
        {
            var gameMetadata = Builder<GameMetadata>.CreateNew().BuildNew();

            Db.Insert(gameMetadata);

            var game = Builder<Game>.CreateNew()
                                              .With(b => b.GameMetadataId = gameMetadata.Id)
                                              .BuildNew();

            Db.Insert(game);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_not_delete_unorphaned_game_metadata_items_for_lists()
        {
            var gameMetadata = Builder<GameMetadata>.CreateNew().BuildNew();

            Db.Insert(gameMetadata);

            var game = Builder<ImportListGame>.CreateNew()
                                              .With(b => b.GameMetadataId = gameMetadata.Id)
                                              .BuildNew();

            Db.Insert(game);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
