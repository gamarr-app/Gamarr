using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Games
{
    /// <summary>
    /// Represents a gaming platform (console/PC/etc.)
    /// </summary>
    public enum GamePlatformCategory
    {
        Console = 1,
        Arcade = 2,
        Platform = 3,
        OperatingSystem = 4,
        PortableConsole = 5,
        Computer = 6
    }

    /// <summary>
    /// Common platform families for filtering
    /// </summary>
    public enum PlatformFamily
    {
        Unknown = 0,
        PC = 1,
        PlayStation = 2,
        Xbox = 3,
        Nintendo = 4,
        Sega = 5,
        Atari = 6,
        Mobile = 7,
        Linux = 8,
        Mac = 9,
        NintendoSwitch = 10,
        NintendoWiiU = 11,
        NintendoWii = 12,
        Nintendo3DS = 13,
        NintendoDS = 14,
        NintendoGBA = 15,
        NintendoGB = 16,
        NintendoGBC = 17,
        NintendoNES = 18,
        NintendoSNES = 19,
        NintendoN64 = 20,
        NintendoFDS = 21,
        NintendoVirtualBoy = 22,
        NintendoPokemonMini = 23,
        NintendoDSi = 24,
        SonyPS3 = 25,
        SonyPSP = 26,
        SonyPSVita = 27
    }

    /// <summary>
    /// Represents a platform a game can be released on
    /// </summary>
    public class GamePlatform
    {
        public int IgdbId { get; set; }
        public int RawgId { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string Slug { get; set; }
        public PlatformFamily Family { get; set; }
        public GamePlatformCategory Category { get; set; }
        public int? Generation { get; set; }

        /// <summary>
        /// Maps IGDB platform family ID to our PlatformFamily enum.
        /// These are the five families IGDB actually defines (verified against
        /// /v4/platform_families): 1 PlayStation, 2 Xbox, 3 Sega, 4 Linux,
        /// 5 Nintendo. There is no Atari family, and 4 is Linux rather than
        /// Nintendo — the previous guesses sent every Nintendo platform that
        /// isn't listed by id below (GameCube, Switch 2, Virtual Boy, ...) to
        /// Atari, and every unlisted Linux-family one to Nintendo.
        /// </summary>
        public static PlatformFamily MapPlatformFamily(int? igdbFamilyId)
        {
            return igdbFamilyId switch
            {
                1 => PlatformFamily.PlayStation,
                2 => PlatformFamily.Xbox,
                3 => PlatformFamily.Sega,
                4 => PlatformFamily.Linux,
                5 => PlatformFamily.Nintendo,
                _ => PlatformFamily.Unknown
            };
        }

        /// <summary>
        /// Maps a specific IGDB platform to its family. IGDB's own
        /// platform_family is far too coarse on its own — every Nintendo
        /// console shares family id 4 — so the platform id is checked first and
        /// the family is only a fallback for platforms we don't list.
        /// </summary>
        public static PlatformFamily MapPlatformFamily(int igdbPlatformId, int? igdbFamilyId)
        {
            var family = igdbPlatformId switch
            {
                CommonPlatforms.Windows => PlatformFamily.PC,
                CommonPlatforms.Linux => PlatformFamily.Linux,
                CommonPlatforms.Mac => PlatformFamily.Mac,
                CommonPlatforms.PS5 or CommonPlatforms.PS4 => PlatformFamily.PlayStation,
                CommonPlatforms.PS3 => PlatformFamily.SonyPS3,
                CommonPlatforms.PSP => PlatformFamily.SonyPSP,
                CommonPlatforms.PSVita => PlatformFamily.SonyPSVita,
                CommonPlatforms.XboxSeriesX or CommonPlatforms.XboxOne or CommonPlatforms.Xbox360 or CommonPlatforms.Xbox => PlatformFamily.Xbox,
                CommonPlatforms.Switch => PlatformFamily.NintendoSwitch,
                CommonPlatforms.WiiU => PlatformFamily.NintendoWiiU,
                CommonPlatforms.Wii => PlatformFamily.NintendoWii,
                CommonPlatforms.Nintendo3DS or CommonPlatforms.New3DS => PlatformFamily.Nintendo3DS,
                CommonPlatforms.NintendoDS => PlatformFamily.NintendoDS,
                CommonPlatforms.NintendoDSi => PlatformFamily.NintendoDSi,
                CommonPlatforms.GameBoyAdvance => PlatformFamily.NintendoGBA,
                CommonPlatforms.GameBoyColor => PlatformFamily.NintendoGBC,
                CommonPlatforms.GameBoy => PlatformFamily.NintendoGB,
                CommonPlatforms.NES or CommonPlatforms.Famicom => PlatformFamily.NintendoNES,
                CommonPlatforms.SNES or CommonPlatforms.SuperFamicom => PlatformFamily.NintendoSNES,
                CommonPlatforms.N64 => PlatformFamily.NintendoN64,
                CommonPlatforms.FamicomDiskSystem => PlatformFamily.NintendoFDS,
                CommonPlatforms.VirtualBoy => PlatformFamily.NintendoVirtualBoy,
                CommonPlatforms.PokemonMini => PlatformFamily.NintendoPokemonMini,
                CommonPlatforms.Android or CommonPlatforms.IOS => PlatformFamily.Mobile,
                _ => PlatformFamily.Unknown
            };

            return family != PlatformFamily.Unknown ? family : MapPlatformFamily(igdbFamilyId);
        }

        /// <summary>
        /// The single family a set of platforms unambiguously belongs to, or
        /// Unknown when it spans more than one. Multiplatform titles stay
        /// Unknown on purpose: Unknown means "any" to PlatformSpecification,
        /// and pinning one arbitrarily would filter out valid releases.
        /// </summary>
        public static PlatformFamily UnambiguousFamily(IEnumerable<GamePlatform> platforms)
        {
            if (platforms == null)
            {
                return PlatformFamily.Unknown;
            }

            var families = platforms.Select(p => p.Family)
                                    .Where(f => f != PlatformFamily.Unknown)
                                    .Distinct()
                                    .ToList();

            return families.Count == 1 ? families[0] : PlatformFamily.Unknown;
        }

        public static bool IsNintendoFamily(PlatformFamily platform)
        {
            return platform is PlatformFamily.Nintendo or
                PlatformFamily.NintendoSwitch or
                PlatformFamily.NintendoWiiU or
                PlatformFamily.NintendoWii or
                PlatformFamily.Nintendo3DS or
                PlatformFamily.NintendoDSi or
                PlatformFamily.NintendoDS or
                PlatformFamily.NintendoGBA or
                PlatformFamily.NintendoGB or
                PlatformFamily.NintendoGBC or
                PlatformFamily.NintendoNES or
                PlatformFamily.NintendoSNES or
                PlatformFamily.NintendoN64 or
                PlatformFamily.NintendoFDS or
                PlatformFamily.NintendoVirtualBoy or
                PlatformFamily.NintendoPokemonMini;
        }

        public static bool IsPlayStationFamily(PlatformFamily platform)
        {
            return platform is PlatformFamily.PlayStation or
                PlatformFamily.SonyPS3 or
                PlatformFamily.SonyPSP or
                PlatformFamily.SonyPSVita;
        }

        public static bool PlatformMatches(PlatformFamily wanted, PlatformFamily actual)
        {
            if (wanted == actual)
            {
                return true;
            }

            return (IsNintendoFamily(wanted) && actual == PlatformFamily.Nintendo) ||
                (wanted == PlatformFamily.Nintendo && IsNintendoFamily(actual)) ||
                (IsPlayStationFamily(wanted) && actual == PlatformFamily.PlayStation) ||
                (wanted == PlatformFamily.PlayStation && IsPlayStationFamily(actual));
        }

        /// <summary>
        /// Common IGDB Platform IDs for reference
        /// </summary>
        public static class CommonPlatforms
        {
            public const int Windows = 6;
            public const int Linux = 3;
            public const int Mac = 14;
            public const int PS5 = 167;
            public const int PS4 = 48;
            public const int PS3 = 9;
            public const int PSP = 38;
            public const int PSVita = 46;
            public const int XboxSeriesX = 169;
            public const int XboxOne = 49;
            public const int Xbox360 = 12;
            public const int Xbox = 11;
            public const int Switch = 130;
            public const int WiiU = 41;
            public const int Wii = 5;
            public const int Nintendo3DS = 37;
            public const int NintendoDS = 20;
            public const int GameBoyAdvance = 24;
            public const int GameBoyColor = 22;
            public const int GameBoy = 33;
            public const int NES = 18;
            public const int SNES = 19;
            public const int N64 = 4;
            public const int Android = 34;
            public const int IOS = 39;

            // Nintendo hardware that has its own PlatformFamily value but was
            // previously only reachable through the (wrong) family fallback.
            public const int New3DS = 137;
            public const int NintendoDSi = 159;
            public const int Famicom = 99;
            public const int SuperFamicom = 58;
            public const int FamicomDiskSystem = 51;
            public const int VirtualBoy = 87;
            public const int PokemonMini = 166;

            // No dedicated PlatformFamily value; these resolve to the generic
            // Nintendo family via platform_family id 5, which PlatformMatches
            // already treats as compatible with any Nintendo console.
            public const int GameCube = 21;
            public const int Switch2 = 508;
        }
    }
}
