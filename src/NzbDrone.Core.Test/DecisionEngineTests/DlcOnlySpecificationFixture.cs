using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class DlcOnlySpecificationFixture : CoreTest<DlcOnlySpecification>
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                Release = new ReleaseInfo
                {
                    Title = "Game.Title.2023"
                },
                ParsedGameInfo = new ParsedGameInfo
                {
                    ContentType = ReleaseContentType.Unknown
                }
            };
        }

        [Test]
        public void should_accept_when_parsed_info_is_null()
        {
            _remoteGame.ParsedGameInfo = null;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_with_dlc_only_reason_when_content_type_is_dlc_only()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.DlcOnly;

            var result = Subject.IsSatisfiedBy(_remoteGame, null);

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.DlcOnly);
        }

        [Test]
        public void should_reject_with_update_only_reason_when_content_type_is_update_only()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.UpdateOnly;

            var result = Subject.IsSatisfiedBy(_remoteGame, null);

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.UpdateOnly);
        }

        [Test]
        public void should_reject_with_season_pass_only_reason_when_content_type_is_season_pass()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.SeasonPass;

            var result = Subject.IsSatisfiedBy(_remoteGame, null);

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.SeasonPassOnly);
        }

        [Test]
        public void should_accept_for_expansion()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.Expansion;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_for_full_game()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.BaseGame;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_for_unknown_content_type()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.Unknown;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }
    }
}
