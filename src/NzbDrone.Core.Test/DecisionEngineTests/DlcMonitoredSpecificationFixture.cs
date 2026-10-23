using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
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
            var decision = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

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

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
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

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_during_interactive_search()
        {
            var criteria = new GameSearchCriteria { UserInvokedSearch = true, InteractiveSearch = true };

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation(false, criteria)).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_during_non_interactive_user_invoked_search()
        {
            // A game search / "Search All" / missing search sets UserInvokedSearch
            // but not InteractiveSearch, and must still respect the slot flag (#214).
            var criteria = new GameSearchCriteria { UserInvokedSearch = true, InteractiveSearch = false };

            var decision = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation(false, criteria));

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.DlcNotMonitored);
        }

        [Test]
        public void should_accept_monitored_slot_during_non_interactive_user_invoked_search()
        {
            // #210 queues component searches only for monitored slots; those must
            // still come through now that the slot flag is enforced during search.
            GivenSlots(new GameComponent
            {
                ComponentType = GameComponentType.Dlc,
                Title = "The Blood Price",
                Monitored = true
            });

            var criteria = new GameSearchCriteria { UserInvokedSearch = true, InteractiveSearch = false };

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation(false, criteria)).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_non_dlc_releases()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.UpdateOnly;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();

            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.BaseGame;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }
    }
}
