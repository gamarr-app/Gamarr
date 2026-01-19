using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Credits;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests.CreditTests
{
    [TestFixture]
    public class CreditRepositoryFixture : DbTest<CreditRepository, Credit>
    {
        private Game _game1;
        private Game _game2;

        [SetUp]
        public void Setup()
        {
            _game1 = Builder<Game>.CreateNew()
                                     .With(s => s.Id = 7)
                                     .Build();

            _game2 = Builder<Game>.CreateNew()
                                     .With(s => s.Id = 8)
                                     .Build();
        }

        [Test]
        public void should_delete_credits_by_gameId()
        {
            var credits = Builder<Credit>.CreateListOfSize(5)
                .TheFirst(1)
                .With(c => c.Id = 0)
                .With(c => c.GameMetadataId = _game2.Id)
                .TheRest()
                .With(c => c.Id = 0)
                .With(c => c.GameMetadataId = _game1.Id)
                .BuildListOfNew();

            Db.InsertMany(credits);

            Subject.DeleteForGames(new List<int> { _game1.Id });

            var removedGameCredits = Subject.FindByGameMetadataId(_game1.Id);
            var nonRemovedGameCredits = Subject.FindByGameMetadataId(_game2.Id);

            removedGameCredits.Should().HaveCount(0);
            nonRemovedGameCredits.Should().HaveCount(1);
        }
    }
}
