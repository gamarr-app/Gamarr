using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class RemovedGameCheckFixture : CoreTest<RemovedGameCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("Some Warning Message");
        }

        private void GivenGame(int amount, int deleted)
        {
            List<Game> game;

            if (amount == 0)
            {
                game = new List<Game>();
            }
            else if (deleted == 0)
            {
                game = Builder<Game>.CreateListOfSize(amount)
                    .All()
                    .With(v => v.GameMetadata.Value.Status = GameStatusType.Released)
                    .BuildList();
            }
            else
            {
                game = Builder<Game>.CreateListOfSize(amount)
                    .All()
                    .With(v => v.GameMetadata.Value.Status = GameStatusType.Released)
                    .Random(deleted)
                    .With(v => v.GameMetadata.Value.Status = GameStatusType.Deleted)
                    .BuildList();
            }

            Mocker.GetMock<IGameService>()
                .Setup(v => v.GetAllGames())
                .Returns(game);
        }

        [Test]
        public void should_return_error_if_game_no_longer_on_igdb()
        {
            GivenGame(4, 1);

            Subject.Check().ShouldBeError();
        }

        [Test]
        public void should_return_error_if_multiple_game_no_longer_on_igdb()
        {
            GivenGame(4, 2);

            Subject.Check().ShouldBeError();
        }

        [Test]
        public void should_return_ok_if_all_game_still_on_igdb()
        {
            GivenGame(4, 0);

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_ok_if_no_game_exist()
        {
            GivenGame(0, 0);

            Subject.Check().ShouldBeOk();
        }
    }
}
