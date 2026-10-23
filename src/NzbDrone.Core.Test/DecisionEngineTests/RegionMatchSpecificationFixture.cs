using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    // #213: an update or DLC of a different regional edition cannot be applied
    // to the installed base game, so it must not be grabbed.
    [TestFixture]
    public class RegionMatchSpecificationFixture : CoreTest<RegionMatchSpecification>
    {
        private RemoteGame _remoteGame;
        private GameFile _baseFile;

        [SetUp]
        public void Setup()
        {
            _baseFile = new GameFile
            {
                Id = 1,
                RelativePath = "Aaaaaaaa.2.EUR.NSW-GROUP.nsp",
                SceneName = "Aaaaaaaa.2.EUR.NSW-GROUP"
            };

            _remoteGame = new RemoteGame
            {
                Game = new Game
                {
                    Id = 7,
                    GameFileId = 1,
                    GameFile = _baseFile
                },
                Release = new ReleaseInfo { Title = "Aaaaaaaa 2 Update v5.5.2 USA NSW-GROUP" },
                ParsedGameInfo = new ParsedGameInfo
                {
                    GameTitles = new List<string> { "Aaaaaaaa 2" },
                    ContentType = ReleaseContentType.UpdateOnly
                }
            };
        }

        private DownloadSpecDecision Decide()
        {
            return Subject.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());
        }

        [Test]
        public void should_reject_an_update_from_a_different_region()
        {
            var decision = Decide();

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.RegionMismatch);
            decision.Message.Should().Contain("EUR");
            decision.Message.Should().Contain("USA");
        }

        [Test]
        public void should_accept_an_update_from_the_same_region()
        {
            _remoteGame.Release.Title = "Aaaaaaaa 2 Update v5.5.2 EUR NSW-GROUP";

            Decide().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_dlc_from_a_different_region()
        {
            // The check is not update-only: regional DLC ids follow the regional
            // base game the same way.
            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.DlcOnly;
            _remoteGame.Release.Title = "Aaaaaaaa 2 Season Pass DLC JPN NSW-GROUP";

            var decision = Decide();

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.RegionMismatch);
        }

        [TestCase(ReleaseContentType.SeasonPass)]
        [TestCase(ReleaseContentType.Expansion)]
        public void should_reject_other_add_on_types_from_a_different_region(ReleaseContentType contentType)
        {
            _remoteGame.ParsedGameInfo.ContentType = contentType;

            Decide().Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_a_release_with_no_region_tag()
        {
            // Most titles are a single worldwide edition and are posted without a
            // region; a false reject here is worse than the bug.
            _remoteGame.Release.Title = "Aaaaaaaa 2 Update v5.5.2 NSW-GROUP";

            Decide().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_the_base_file_states_no_region()
        {
            _baseFile.SceneName = "Aaaaaaaa.2.NSW-GROUP";

            Decide().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_the_base_file_has_no_scene_name()
        {
            _baseFile.SceneName = null;
            _baseFile.RelativePath = "Aaaaaaaa 2.nsp";

            Decide().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_fall_back_to_the_file_name_when_there_is_no_scene_name()
        {
            // GetSceneOrFileName: a scanned-in file keeps its region only in the
            // file name.
            _baseFile.SceneName = null;
            _baseFile.RelativePath = "Aaaaaaaa.2.EUR.NSW-GROUP.nsp";

            Decide().Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_when_the_game_has_no_base_file()
        {
            _remoteGame.Game.GameFile = null;
            _remoteGame.Game.GameFileId = 0;

            Decide().Accepted.Should().BeTrue();
        }

        [TestCase(ReleaseContentType.BaseGame)]
        [TestCase(ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase(ReleaseContentType.Unknown)]
        [TestCase(ReleaseContentType.Demo)]
        public void should_not_gate_a_release_that_is_not_an_add_on(ReleaseContentType contentType)
        {
            // A base-game release is not an add-on to anything, so a user moving
            // to another edition must not be blocked by the one on disk.
            _remoteGame.ParsedGameInfo.ContentType = contentType;

            Decide().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_a_multi_region_release_that_covers_the_base_region()
        {
            _remoteGame.Release.Title = "Aaaaaaaa 2 Update v5.5.2 USA EUR NSW-GROUP";

            Decide().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_a_pal_release_for_a_eur_base()
        {
            _remoteGame.Release.Title = "Aaaaaaaa 2 Update v5.5.2 PAL NSW-GROUP";

            Decide().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_a_jap_release_for_a_jpn_base()
        {
            _baseFile.SceneName = "Aaaaaaaa.2.JPN.NSW-GROUP";
            _remoteGame.Release.Title = "Aaaaaaaa 2 Update v5.5.2 JAP NSW-GROUP";

            Decide().Accepted.Should().BeTrue();
        }
    }
}
