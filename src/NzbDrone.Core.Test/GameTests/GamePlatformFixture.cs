using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class GamePlatformFixture : CoreTest
    {
        // IGDB's platform_family is 4 for every Nintendo console, so the
        // platform id has to drive the mapping.
        [TestCase(GamePlatform.CommonPlatforms.Switch, 4, PlatformFamily.NintendoSwitch)]
        [TestCase(GamePlatform.CommonPlatforms.WiiU, 4, PlatformFamily.NintendoWiiU)]
        [TestCase(GamePlatform.CommonPlatforms.Nintendo3DS, 4, PlatformFamily.Nintendo3DS)]
        [TestCase(GamePlatform.CommonPlatforms.Windows, null, PlatformFamily.PC)]
        [TestCase(GamePlatform.CommonPlatforms.Linux, null, PlatformFamily.Linux)]
        [TestCase(GamePlatform.CommonPlatforms.Mac, null, PlatformFamily.Mac)]
        [TestCase(GamePlatform.CommonPlatforms.PS5, 1, PlatformFamily.PlayStation)]
        [TestCase(GamePlatform.CommonPlatforms.PS3, 1, PlatformFamily.SonyPS3)]
        [TestCase(GamePlatform.CommonPlatforms.PSVita, 1, PlatformFamily.SonyPSVita)]
        [TestCase(GamePlatform.CommonPlatforms.XboxSeriesX, 2, PlatformFamily.Xbox)]
        public void should_map_igdb_platform_id_to_specific_family(int platformId, int? familyId, PlatformFamily expected)
        {
            GamePlatform.MapPlatformFamily(platformId, familyId).Should().Be(expected);
        }

        [Test]
        public void should_fall_back_to_igdb_platform_family_for_unlisted_platform()
        {
            // 87 is Virtual Boy - not in CommonPlatforms, but IGDB says Nintendo
            GamePlatform.MapPlatformFamily(87, 4).Should().Be(PlatformFamily.Nintendo);
        }

        [Test]
        public void should_return_unknown_when_platform_and_family_are_both_unlisted()
        {
            GamePlatform.MapPlatformFamily(9999, null).Should().Be(PlatformFamily.Unknown);
        }

        [Test]
        public void should_return_the_single_family_a_game_is_released_on()
        {
            var platforms = new List<GamePlatform>
            {
                new GamePlatform { IgdbId = GamePlatform.CommonPlatforms.Switch, Family = PlatformFamily.NintendoSwitch }
            };

            GamePlatform.UnambiguousFamily(platforms).Should().Be(PlatformFamily.NintendoSwitch);
        }

        [Test]
        public void should_return_unknown_for_a_multiplatform_game()
        {
            var platforms = new List<GamePlatform>
            {
                new GamePlatform { IgdbId = GamePlatform.CommonPlatforms.Windows, Family = PlatformFamily.PC },
                new GamePlatform { IgdbId = GamePlatform.CommonPlatforms.Switch, Family = PlatformFamily.NintendoSwitch }
            };

            GamePlatform.UnambiguousFamily(platforms).Should().Be(PlatformFamily.Unknown);
        }

        [Test]
        public void should_ignore_unknown_families_when_collapsing()
        {
            var platforms = new List<GamePlatform>
            {
                new GamePlatform { Family = PlatformFamily.Unknown },
                new GamePlatform { IgdbId = GamePlatform.CommonPlatforms.Switch, Family = PlatformFamily.NintendoSwitch }
            };

            GamePlatform.UnambiguousFamily(platforms).Should().Be(PlatformFamily.NintendoSwitch);
        }

        [Test]
        public void should_return_unknown_for_no_platforms()
        {
            GamePlatform.UnambiguousFamily(null).Should().Be(PlatformFamily.Unknown);
            GamePlatform.UnambiguousFamily(new List<GamePlatform>()).Should().Be(PlatformFamily.Unknown);
        }
    }
}
