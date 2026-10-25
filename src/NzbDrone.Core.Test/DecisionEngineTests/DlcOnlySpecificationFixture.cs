using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
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

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_with_dlc_only_reason_when_content_type_is_dlc_only()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.DlcOnly;

            var result = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.DlcOnly);
        }

        [TestCase(ReleaseContentType.DlcOnly)]
        [TestCase(ReleaseContentType.UpdateOnly)]
        [TestCase(ReleaseContentType.SeasonPass)]
        public void should_accept_base_requiring_release_when_base_game_is_on_disk(ReleaseContentType contentType)
        {
            // Updates/DLC import alongside the base (#149 phase 0), so they're
            // grabbable whenever a base exists to apply them to.
            _remoteGame.Game = new NzbDrone.Core.Games.Game { GameFileId = 1 };
            _remoteGame.ParsedGameInfo.ContentType = contentType;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [TestCase(ReleaseContentType.DlcOnly)]
        [TestCase(ReleaseContentType.UpdateOnly)]
        [TestCase(ReleaseContentType.SeasonPass)]
        public void should_reject_base_requiring_release_when_no_base_game_on_disk(ReleaseContentType contentType)
        {
            _remoteGame.Game = new NzbDrone.Core.Games.Game { GameFileId = 0 };
            _remoteGame.ParsedGameInfo.ContentType = contentType;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        // #216: the post-import update search runs while the base file row
        // already exists but the game's primary pointer may not have caught up,
        // and every update was rejected as "none on disk" because of it.
        [TestCase(ReleaseContentType.DlcOnly)]
        [TestCase(ReleaseContentType.UpdateOnly)]
        [TestCase(ReleaseContentType.SeasonPass)]
        public void should_accept_base_requiring_release_when_a_base_file_exists_but_primary_is_unassigned(ReleaseContentType contentType)
        {
            _remoteGame.Game = new NzbDrone.Core.Games.Game { Id = 7, GameFileId = 0 };
            _remoteGame.ParsedGameInfo.ContentType = contentType;

            GivenFiles(new GameFile { Id = 1, GameId = 7, RelativePath = string.Empty });

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [TestCase(ReleaseContentType.DlcOnly)]
        [TestCase(ReleaseContentType.UpdateOnly)]
        [TestCase(ReleaseContentType.SeasonPass)]
        public void should_reject_base_requiring_release_when_only_update_and_dlc_files_exist(ReleaseContentType contentType)
        {
            _remoteGame.Game = new NzbDrone.Core.Games.Game { Id = 7, GameFileId = 0 };
            _remoteGame.ParsedGameInfo.ContentType = contentType;

            GivenFiles(new GameFile { Id = 1, GameId = 7, RelativePath = "Updates/v1.0.2" },
                       new GameFile { Id = 2, GameId = 7, RelativePath = "DLC/The Blood Price" });

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        private void GivenFiles(params GameFile[] files)
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(It.IsAny<int>()))
                  .Returns(new List<GameFile>(files));
        }

        [Test]
        public void should_reject_with_update_only_reason_when_content_type_is_update_only()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.UpdateOnly;

            var result = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.UpdateOnly);
        }

        [Test]
        public void should_reject_with_season_pass_only_reason_when_content_type_is_season_pass()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.SeasonPass;

            var result = Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.SeasonPassOnly);
        }

        [Test]
        public void should_accept_for_expansion()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.Expansion;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_for_full_game()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.BaseGame;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_for_unknown_content_type()
        {
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.Unknown;

            Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }
    }
}
