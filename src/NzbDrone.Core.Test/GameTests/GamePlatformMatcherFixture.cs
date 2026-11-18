using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class GamePlatformMatcherFixture : CoreTest
    {
        private static Game GivenGame(int id, PlatformFamily platform)
        {
            return new Game { Id = id, Platform = platform, GameMetadata = new GameMetadata { Title = "Zelda" } };
        }

        [TestCase(PlatformFamily.NintendoSwitch, 1)]
        [TestCase(PlatformFamily.NintendoN64, 2)]
        [TestCase(PlatformFamily.PC, 3)]
        [TestCase(PlatformFamily.Unknown, 3)]
        public void should_pick_the_entry_for_the_release_platform(PlatformFamily releasePlatform, int expectedId)
        {
            var games = new List<Game>
            {
                GivenGame(1, PlatformFamily.NintendoSwitch),
                GivenGame(2, PlatformFamily.NintendoN64),
                GivenGame(3, PlatformFamily.PC)
            };

            GamePlatformMatcher.SelectForReleasePlatform(games, releasePlatform).Id.Should().Be(expectedId);
        }

        [Test]
        public void should_prefer_exact_platform_over_family_match()
        {
            var games = new List<Game> { GivenGame(1, PlatformFamily.Nintendo), GivenGame(2, PlatformFamily.NintendoSwitch) };

            GamePlatformMatcher.SelectForReleasePlatform(games, PlatformFamily.NintendoSwitch).Id.Should().Be(2);
        }

        [Test]
        public void should_fall_back_to_unknown_entry_when_no_entry_claims_the_platform()
        {
            var games = new List<Game> { GivenGame(1, PlatformFamily.Unknown), GivenGame(2, PlatformFamily.NintendoSwitch) };

            GamePlatformMatcher.SelectForReleasePlatform(games, PlatformFamily.PlayStation).Id.Should().Be(1);
        }

        [Test]
        public void should_return_null_for_a_same_platform_pair()
        {
            var games = new List<Game> { GivenGame(1, PlatformFamily.NintendoSwitch), GivenGame(2, PlatformFamily.NintendoSwitch) };

            GamePlatformMatcher.SelectForReleasePlatform(games, PlatformFamily.NintendoSwitch).Should().BeNull();
        }

        [Test]
        public void should_return_null_when_no_entry_fits_the_release()
        {
            var games = new List<Game> { GivenGame(1, PlatformFamily.NintendoSwitch), GivenGame(2, PlatformFamily.NintendoN64) };

            GamePlatformMatcher.SelectForReleasePlatform(games, PlatformFamily.PlayStation).Should().BeNull();
        }
    }
}
