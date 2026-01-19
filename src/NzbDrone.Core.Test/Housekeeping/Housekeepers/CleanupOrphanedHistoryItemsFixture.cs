using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedHistoryItemsFixture : DbTest<CleanupOrphanedHistoryItems, GameHistory>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                   .BuildNew();
        }

        private void GivenSeries()
        {
            Db.Insert(_game);
        }

        [Test]
        public void should_delete_orphaned_items()
        {
            var history = Builder<GameHistory>.CreateNew()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.Languages = new List<Language>())
                                                  .BuildNew();
            Db.Insert(history);

            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned()
        {
            GivenSeries();

            var history = Builder<GameHistory>.CreateNew()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.Languages = new List<Language>())
                                                  .With(h => h.GameId = _game.Id)
                                                  .BuildNew();
            Db.Insert(history);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
