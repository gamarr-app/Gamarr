using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedCollectionsFixture : DbTest<CleanupOrphanedCollections, GameCollection>
    {
        [Test]
        public void should_delete_orphaned_collection_item()
        {
            var collection = Builder<GameCollection>.CreateNew()
                                              .With(h => h.Id = 3)
                                              .With(h => h.IgdbId = 123456)
                                              .With(h => h.Title = "Some Credit")
                                              .BuildNew();

            Db.Insert(collection);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_delete_orphaned_collection_with_meta_but_no_game_items()
        {
            var collection = Builder<GameCollection>.CreateNew()
                                              .With(h => h.Id = 3)
                                              .With(h => h.IgdbId = 123456)
                                              .With(h => h.Title = "Some Credit")
                                              .BuildNew();

            Db.Insert(collection);

            var game = Builder<GameMetadata>.CreateNew().With(m => m.CollectionIgdbId = collection.IgdbId).BuildNew();

            Db.Insert(game);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(0);
        }

        [Test]
        public void should_not_delete_unorphaned_collection()
        {
            var collection = Builder<GameCollection>.CreateNew()
                                              .With(h => h.Id = 3)
                                              .With(h => h.IgdbId = 123456)
                                              .With(h => h.Title = "Some Credit")
                                              .BuildNew();

            Db.Insert(collection);

            var gameMeta = Builder<GameMetadata>.CreateNew().With(m => m.CollectionIgdbId = collection.IgdbId).BuildNew();
            Db.Insert(gameMeta);

            var game = Builder<Game>.CreateNew().With(m => m.GameMetadataId = gameMeta.Id).BuildNew();
            Db.Insert(game);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
