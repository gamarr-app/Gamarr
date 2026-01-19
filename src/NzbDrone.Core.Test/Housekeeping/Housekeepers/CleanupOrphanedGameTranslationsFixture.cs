using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedGameTranslationsFixture : DbTest<CleanupOrphanedGameTranslations, GameTranslation>
    {
        [Test]
        public void should_delete_orphaned_game_translation_items()
        {
            var translation = Builder<GameTranslation>.CreateNew()
                                              .With(h => h.GameMetadataId = default)
                                              .With(h => h.Language = Language.English)
                                              .BuildNew();

            Db.Insert(translation);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_game_translation_items()
        {
            var gameMetadata = Builder<GameMetadata>.CreateNew().BuildNew();

            Db.Insert(gameMetadata);

            var translation = Builder<GameTranslation>.CreateNew()
                                              .With(h => h.GameMetadataId = default)
                                              .With(h => h.Language = Language.English)
                                              .With(b => b.GameMetadataId = gameMetadata.Id)
                                              .BuildNew();

            Db.Insert(translation);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
