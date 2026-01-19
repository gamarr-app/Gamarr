using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedAlternativeTitleFixture : DbTest<CleanupOrphanedAlternativeTitles, AlternativeTitle>
    {
        [Test]
        public void should_delete_orphaned_alternative_title_items()
        {
            var altTitle = Builder<AlternativeTitle>.CreateNew()
                                              .With(h => h.GameMetadataId = default)
                                              .BuildNew();

            Db.Insert(altTitle);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_alternative_title_items()
        {
            var gameMetadata = Builder<GameMetadata>.CreateNew().BuildNew();

            Db.Insert(gameMetadata);

            var altTitle = Builder<AlternativeTitle>.CreateNew()
                                              .With(h => h.GameMetadataId = default)
                                              .With(b => b.GameMetadataId = gameMetadata.Id)
                                              .BuildNew();

            Db.Insert(altTitle);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
