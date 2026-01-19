using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedImportListGamesFixture : DbTest<CleanupOrphanedImportListGames, ImportListGame>
    {
        private ImportListDefinition _importList;

        [SetUp]
        public void Setup()
        {
            _importList = Builder<ImportListDefinition>.CreateNew()
                                                       .BuildNew();
        }

        private void GivenImportList()
        {
            Db.Insert(_importList);
        }

        [Test]
        public void should_delete_orphaned_importlistgames()
        {
            var status = Builder<ImportListGame>.CreateNew()
                                                 .With(h => h.ListId = _importList.Id)
                                                 .BuildNew();
            Db.Insert(status);

            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_importlistgames()
        {
            GivenImportList();

            var gameMetadata = Builder<GameMetadata>.CreateNew().BuildNew();

            Db.Insert(gameMetadata);

            var status = Builder<ImportListGame>.CreateNew()
                                                 .With(h => h.ListId = _importList.Id)
                                                 .With(b => b.GameMetadataId = gameMetadata.Id)
                                                 .BuildNew();
            Db.Insert(status);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.Should().Contain(h => h.ListId == _importList.Id);
        }
    }
}
