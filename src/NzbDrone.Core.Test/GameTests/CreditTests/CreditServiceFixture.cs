using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Credits;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests.AlternativeTitleServiceTests
{
    [TestFixture]
    public class CreditServiceFixture : CoreTest<CreditService>
    {
        private Credit _credit1;
        private Credit _credit2;
        private Credit _credit3;

        private GameMetadata _game;

        [SetUp]
        public void Setup()
        {
            var credits = Builder<Credit>.CreateListOfSize(3)
                                         .All()
                                         .With(t => t.GameMetadataId = 0).Build();

            _credit1 = credits[0];
            _credit2 = credits[1];
            _credit3 = credits[2];

            _game = Builder<GameMetadata>.CreateNew().With(m => m.Id = 1).Build();
        }

        private void GivenExistingCredits(params Credit[] credits)
        {
            Mocker.GetMock<ICreditRepository>().Setup(r => r.FindByGameMetadataId(_game.Id))
                .Returns(credits.ToList());
        }

        [Test]
        public void should_update_insert_remove_titles()
        {
            var titles = new List<Credit> { _credit2, _credit3 };
            var updates = new List<Credit> { _credit2 };
            var deletes = new List<Credit> { _credit1 };
            var inserts = new List<Credit> { _credit3 };

            GivenExistingCredits(_credit1, _credit2);

            Subject.UpdateCredits(titles, _game);

            Mocker.GetMock<ICreditRepository>().Verify(r => r.InsertMany(inserts), Times.Once());
            Mocker.GetMock<ICreditRepository>().Verify(r => r.UpdateMany(new List<Credit>()), Times.Once());
            Mocker.GetMock<ICreditRepository>().Verify(r => r.DeleteMany(deletes), Times.Once());
        }

        [Test]
        public void should_not_insert_duplicates()
        {
            GivenExistingCredits();
            var credits = new List<Credit> { _credit1, _credit1 };
            var inserts = new List<Credit> { _credit1 };

            Subject.UpdateCredits(credits, _game);

            Mocker.GetMock<ICreditRepository>().Verify(r => r.InsertMany(inserts), Times.Once());
        }

        [Test]
        public void should_update_game_id()
        {
            GivenExistingCredits();
            var titles = new List<Credit> { _credit1, _credit2 };

            Subject.UpdateCredits(titles, _game);

            _credit1.GameMetadataId.Should().Be(_game.Id);
            _credit2.GameMetadataId.Should().Be(_game.Id);
        }

        [Test]
        public void should_update_with_correct_id()
        {
            var existingCredit = Builder<Credit>.CreateNew().With(t => t.Id = 2).Build();

            GivenExistingCredits(existingCredit);

            var updateCredit = existingCredit.JsonClone();
            updateCredit.Id = 0;

            var result = Subject.UpdateCredits(new List<Credit> { updateCredit }, _game);

            result.Should().HaveCount(1);
            result.First().Id.Should().Be(existingCredit.Id);

            Mocker.GetMock<ICreditRepository>().Verify(r => r.UpdateMany(It.Is<IList<Credit>>(l => l.Count == 0)), Times.Once());
        }
    }
}
