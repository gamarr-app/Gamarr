using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Indexers;
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
                Game = new Game { Id = 1 },
                Release = new ReleaseInfo
                {
                    Title = "Game.Title.2023",
                    DownloadProtocol = DownloadProtocol.Torrent
                },
                ParsedGameInfo = new ParsedGameInfo
                {
                    ContentType = ReleaseContentType.Unknown
                }
            };
        }

        private void WithContentType(ReleaseContentType contentType)
        {
            _remoteGame.ParsedGameInfo.ContentType = contentType;
        }

        [Test]
        public void should_return_true_when_parsed_info_is_null()
        {
            _remoteGame.ParsedGameInfo = null;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_for_unknown_content_type()
        {
            WithContentType(ReleaseContentType.Unknown);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_for_base_game()
        {
            WithContentType(ReleaseContentType.BaseGame);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_for_base_game_with_all_dlc()
        {
            WithContentType(ReleaseContentType.BaseGameWithAllDlc);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_for_expansion()
        {
            WithContentType(ReleaseContentType.Expansion);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_for_dlc_only()
        {
            WithContentType(ReleaseContentType.DlcOnly);

            var result = Subject.IsSatisfiedBy(_remoteGame, null);
            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.DlcOnly);
        }

        [Test]
        public void should_return_false_for_update_only()
        {
            WithContentType(ReleaseContentType.UpdateOnly);

            var result = Subject.IsSatisfiedBy(_remoteGame, null);
            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.UpdateOnly);
        }

        [Test]
        public void should_return_false_for_season_pass()
        {
            WithContentType(ReleaseContentType.SeasonPass);

            var result = Subject.IsSatisfiedBy(_remoteGame, null);
            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.SeasonPassOnly);
        }

        [Test]
        public void should_have_permanent_rejection_type()
        {
            Subject.Type.Should().Be(RejectionType.Permanent);
        }

        [Test]
        public void should_have_default_priority()
        {
            Subject.Priority.Should().Be(SpecificationPriority.Default);
        }
    }
}
