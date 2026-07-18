using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class DlcMonitoredSpecificationFixture : CoreTest<DlcMonitoredSpecification>
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                Game = new Game { Id = 7 },
                Release = new ReleaseInfo { Title = "Hades.The.Blood.Price.DLC-GRP" },
                ParsedGameInfo = new ParsedGameInfo
                {
                    GameTitles = new List<string> { "Hades The Blood Price" },
                    ContentType = ReleaseContentType.DlcOnly
                }
            };

            GivenSlots(new GameComponent
            {
                Id = 1,
                GameId = 7,
                ComponentType = GameComponentType.Dlc,
                Key = "igdb:111",
                Title = "The Blood Price",
                Monitored = false
            });
        }

        private void GivenSlots(params GameComponent[] slots)
        {
            Mocker.GetMock<IGameComponentService>()
                  .Setup(s => s.GetByGame(7))
                  .Returns(new List<GameComponent>(slots));
        }

        [Test]
        public void should_reject_dlc_release_for_unmonitored_slot()
        {
            var decision = Subject.IsSatisfiedBy(_remoteGame, null);

            decision.Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_dlc_release_for_monitored_slot()
        {
            GivenSlots(new GameComponent
            {
                ComponentType = GameComponentType.Dlc,
                Title = "The Blood Price",
                Monitored = true
            });

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_dlc_release_with_no_matching_slot()
        {
            GivenSlots(new GameComponent
            {
                ComponentType = GameComponentType.Dlc,
                Title = "Warm Winds",
                Monitored = true
            });

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_during_user_invoked_search()
        {
            var criteria = new GameSearchCriteria { UserInvokedSearch = true };

            Subject.IsSatisfiedBy(_remoteGame, criteria).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_non_dlc_releases()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.UpdateOnly;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();

            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.BaseGame;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }
    }
}
