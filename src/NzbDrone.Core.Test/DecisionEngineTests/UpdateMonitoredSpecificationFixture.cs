using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    // #203: nothing consulted game.MonitorUpdates or the update component's
    // Monitored flag for an update release, so updates were grabbed with both
    // switched off.
    [TestFixture]
    public class UpdateMonitoredSpecificationFixture : CoreTest<UpdateMonitoredSpecification>
    {
        private RemoteGame _remoteGame;
        private List<GameComponent> _components;

        [SetUp]
        public void Setup()
        {
            _components = new List<GameComponent>();

            _remoteGame = new RemoteGame
            {
                Game = new Game
                {
                    Id = 7,
                    MonitorUpdates = true,
                    GameFileId = 1
                },
                Release = new ReleaseInfo { Title = "Game Update v1.7.0 NSW-GROUP" },
                ParsedGameInfo = new ParsedGameInfo
                {
                    ContentType = ReleaseContentType.UpdateOnly,
                    GameVersion = new GameVersion(1, 7, 0)
                }
            };

            Mocker.GetMock<IGameComponentService>()
                .Setup(s => s.GetByGame(It.IsAny<int>()))
                .Returns(() => _components);
        }

        private void GivenUpdateSlot(string key, bool monitored)
        {
            _components.Add(new GameComponent
            {
                Id = _components.Count + 1,
                GameId = _remoteGame.Game.Id,
                ComponentType = GameComponentType.Update,
                Key = key,
                Title = key,
                Monitored = monitored
            });
        }

        [Test]
        public void should_reject_update_release_when_game_does_not_monitor_updates()
        {
            _remoteGame.Game.MonitorUpdates = false;

            var decision = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.UpdateNotMonitored);
        }

        [Test]
        public void should_reject_update_release_when_game_does_not_monitor_updates_during_user_invoked_search()
        {
            // Turning updates off is about a kind of content, not about the
            // game, so a user-invoked search must not bypass it.
            _remoteGame.Game.MonitorUpdates = false;

            var information = new ReleaseDecisionInformation(false, new GameSearchCriteria { UserInvokedSearch = true });

            Subject.IsSatisfiedBy(_remoteGame, information).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_update_release_when_matching_update_component_is_unmonitored()
        {
            GivenUpdateSlot("v1.7.0", monitored: false);
            GivenUpdateSlot("v4.0.1", monitored: true);

            var decision = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.UpdateNotMonitored);
        }

        [Test]
        public void should_reject_update_release_when_no_update_component_is_monitored()
        {
            // The reporter's state: the installed update component unmonitored
            // and the offered version has no slot of its own.
            GivenUpdateSlot("v4.0.1", monitored: false);

            var decision = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.UpdateNotMonitored);
        }

        [Test]
        public void should_accept_update_release_when_an_update_component_is_monitored()
        {
            GivenUpdateSlot("v4.0.1", monitored: true);

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_update_release_when_game_has_no_update_components_yet()
        {
            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_non_update_release_when_updates_are_not_monitored()
        {
            _remoteGame.Game.MonitorUpdates = false;
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.BaseGame;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_game_is_unknown()
        {
            _remoteGame.Game = null;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }
    }
}
