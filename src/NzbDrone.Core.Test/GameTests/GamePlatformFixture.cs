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
        // IGDB's platform_family is 5 for every Nintendo console, so the
        // platform id has to drive the mapping. (Verified against
        // /v4/platform_families: 1 PlayStation, 2 Xbox, 3 Sega, 4 Linux,
        // 5 Nintendo — and PC/Mac/iOS carry no family at all.)
        [TestCase(GamePlatform.CommonPlatforms.Switch, 5, PlatformFamily.NintendoSwitch)]
        [TestCase(GamePlatform.CommonPlatforms.WiiU, 5, PlatformFamily.NintendoWiiU)]
        [TestCase(GamePlatform.CommonPlatforms.Nintendo3DS, 5, PlatformFamily.Nintendo3DS)]
        [TestCase(GamePlatform.CommonPlatforms.New3DS, 5, PlatformFamily.Nintendo3DS)]
        [TestCase(GamePlatform.CommonPlatforms.NintendoDSi, 5, PlatformFamily.NintendoDSi)]
        [TestCase(GamePlatform.CommonPlatforms.Famicom, 5, PlatformFamily.NintendoNES)]
        [TestCase(GamePlatform.CommonPlatforms.SuperFamicom, 5, PlatformFamily.NintendoSNES)]
        [TestCase(GamePlatform.CommonPlatforms.FamicomDiskSystem, 5, PlatformFamily.NintendoFDS)]
        [TestCase(GamePlatform.CommonPlatforms.VirtualBoy, 5, PlatformFamily.NintendoVirtualBoy)]
        [TestCase(GamePlatform.CommonPlatforms.PokemonMini, 5, PlatformFamily.NintendoPokemonMini)]
        [TestCase(GamePlatform.CommonPlatforms.Android, 4, PlatformFamily.Mobile)]
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
            // GameCube has no PlatformFamily value of its own, but IGDB puts it
            // in family 5 (Nintendo), and PlatformMatches treats the generic
            // Nintendo family as compatible with any Nintendo console.
            GamePlatform.MapPlatformFamily(GamePlatform.CommonPlatforms.GameCube, 5).Should().Be(PlatformFamily.Nintendo);
        }

        // Family 4 is Linux and family 5 is Nintendo. Reading them the other way
        // round (as this code once did) sent every unlisted Nintendo console to
        // Atari and every unlisted Linux-family one to Nintendo.
        [TestCase(5, PlatformFamily.Nintendo)]
        [TestCase(4, PlatformFamily.Linux)]
        [TestCase(3, PlatformFamily.Sega)]
        [TestCase(2, PlatformFamily.Xbox)]
        [TestCase(1, PlatformFamily.PlayStation)]
        [TestCase(null, PlatformFamily.Unknown)]
        [TestCase(6, PlatformFamily.Unknown)]
        public void should_map_igdb_platform_family_ids(int? familyId, PlatformFamily expected)
        {
            GamePlatform.MapPlatformFamily(familyId).Should().Be(expected);
        }

        [Test]
        public void should_not_resolve_an_unlisted_nintendo_console_to_pc_or_atari()
        {
            // Nintendo Switch 2 (508) has no PlatformFamily value of its own.
            var family = GamePlatform.MapPlatformFamily(GamePlatform.CommonPlatforms.Switch2, 5);

            family.Should().Be(PlatformFamily.Nintendo);
            GamePlatform.PlatformMatches(PlatformFamily.NintendoSwitch, family).Should().BeTrue();
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
