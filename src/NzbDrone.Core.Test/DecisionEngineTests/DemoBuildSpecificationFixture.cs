using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class DemoBuildSpecificationFixture : CoreTest<DemoBuildSpecification>
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                Release = new ReleaseInfo
                {
                    Title = "Super.Mario.Odyssey.KIOSK.eShop.INTERNAL.NSW-VENOM"
                },
                ParsedGameInfo = new ParsedGameInfo
                {
                    ContentType = ReleaseContentType.Unknown
                }
            };
        }

        [Test]
        public void should_reject_demo_build()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.Demo;

            var result = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.DemoBuild);
        }

        [Test]
        public void should_reject_demo_build_even_when_the_base_game_is_on_disk()
        {
            // A demo is never an upgrade to the game, so unlike DLC/updates the
            // rejection does not depend on what is already imported.
            _remoteGame.Game = new NzbDrone.Core.Games.Game { GameFileId = 1 };
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.Demo;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_the_reported_release_end_to_end()
        {
            _remoteGame.ParsedGameInfo = Parser.Parser.ParseGameTitle(_remoteGame.Release.Title);

            var result = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.DemoBuild);
        }

        [Test]
        public void should_accept_when_parsed_info_is_null()
        {
            _remoteGame.ParsedGameInfo = null;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [TestCase(ReleaseContentType.Unknown)]
        [TestCase(ReleaseContentType.BaseGame)]
        [TestCase(ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase(ReleaseContentType.DlcOnly)]
        [TestCase(ReleaseContentType.UpdateOnly)]
        [TestCase(ReleaseContentType.SeasonPass)]
        [TestCase(ReleaseContentType.Expansion)]
        public void should_accept_every_other_content_type(ReleaseContentType contentType)
        {
            _remoteGame.ParsedGameInfo.ContentType = contentType;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }
    }
}
