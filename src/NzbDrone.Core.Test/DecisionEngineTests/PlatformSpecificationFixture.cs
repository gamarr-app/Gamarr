using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class PlatformSpecificationFixture : CoreTest<PlatformSpecification>
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                ParsedGameInfo = new ParsedGameInfo
                {
                    Platform = PlatformFamily.PC
                },
                Game = new Game
                {
                    QualityProfile = new QualityProfile
                    {
                        PreferredPlatforms = new List<PlatformFamily> { PlatformFamily.PC }
                    }
                }
            };
        }

        [Test]
        public void should_accept_when_no_restrictions()
        {
            _remoteGame.Game.QualityProfile.PreferredPlatforms = null;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_preferred_platforms_empty()
        {
            _remoteGame.Game.QualityProfile.PreferredPlatforms = new List<PlatformFamily>();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_unknown_platform()
        {
            _remoteGame.ParsedGameInfo.Platform = PlatformFamily.Unknown;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_matching_platform()
        {
            _remoteGame.ParsedGameInfo.Platform = PlatformFamily.PC;
            _remoteGame.Game.QualityProfile.PreferredPlatforms = new List<PlatformFamily>
            {
                PlatformFamily.PC,
                PlatformFamily.PlayStation
            };

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_non_matching_platform()
        {
            _remoteGame.ParsedGameInfo.Platform = PlatformFamily.Nintendo;
            _remoteGame.Game.QualityProfile.PreferredPlatforms = new List<PlatformFamily>
            {
                PlatformFamily.PC,
                PlatformFamily.PlayStation
            };

            var result = Subject.IsSatisfiedBy(_remoteGame, null);

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.WantedPlatform);
        }

        [Test]
        public void should_accept_broad_nintendo_release_for_specific_nintendo_entry()
        {
            _remoteGame.Game.Platform = PlatformFamily.NintendoDS;
            _remoteGame.ParsedGameInfo.Platform = PlatformFamily.Nintendo;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_specific_nintendo_release_for_broad_nintendo_preference()
        {
            _remoteGame.Game.Platform = PlatformFamily.Unknown;
            _remoteGame.ParsedGameInfo.Platform = PlatformFamily.NintendoGBA;
            _remoteGame.Game.QualityProfile.PreferredPlatforms = new List<PlatformFamily>
            {
                PlatformFamily.Nintendo
            };

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_game_boy_color_release_for_broad_nintendo_preference()
        {
            _remoteGame.Game.Platform = PlatformFamily.Unknown;
            _remoteGame.ParsedGameInfo.Platform = PlatformFamily.NintendoGBC;
            _remoteGame.Game.QualityProfile.PreferredPlatforms = new List<PlatformFamily>
            {
                PlatformFamily.Nintendo
            };

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_specific_playstation_release_for_broad_playstation_preference()
        {
            _remoteGame.Game.Platform = PlatformFamily.Unknown;
            _remoteGame.ParsedGameInfo.Platform = PlatformFamily.SonyPSP;
            _remoteGame.Game.QualityProfile.PreferredPlatforms = new List<PlatformFamily>
            {
                PlatformFamily.PlayStation
            };

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }
    }
}
