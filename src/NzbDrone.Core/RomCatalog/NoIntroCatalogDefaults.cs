using System.Collections.Generic;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.RomCatalog
{
    public static class NoIntroCatalogDefaults
    {
        public static readonly IReadOnlyList<NoIntroCatalogSourceSeed> Sources = new List<NoIntroCatalogSourceSeed>
        {
            new NoIntroCatalogSourceSeed("No-Intro Nintendo Game Boy", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo Game Boy Color", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy%20Color.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo Game Boy Advance", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy%20Advance.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo DS", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20DS.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo 3DS", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%203DS.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo 3DS Digital", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%203DS%20%28Digital%29.dat"),
            new NoIntroCatalogSourceSeed("No-Intro New Nintendo 3DS", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20New%20Nintendo%203DS.dat"),
            new NoIntroCatalogSourceSeed("No-Intro New Nintendo 3DS Digital", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20New%20Nintendo%203DS%20%28Digital%29.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo DS Download Play", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20DS%20%28Download%20Play%29.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo DS DSvision SD Cards", "datomatic://system/319"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo Game Boy Advance Multiboot", "datomatic://system/137"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo Game Boy Advance e-Reader", "datomatic://system/41"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo Game Boy Advance Play-Yan", "datomatic://system/148"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo Game Boy Advance Video", "datomatic://system/297"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo Entertainment System", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20Entertainment%20System.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Super Nintendo Entertainment System", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Super%20Nintendo%20Entertainment%20System.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo 64", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%2064.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo 64DD", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%2064DD.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Family Computer Disk System", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Family%20Computer%20Disk%20System.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Virtual Boy", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Virtual%20Boy.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Pokemon Mini", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Pokemon%20Mini.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Satellaview", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Satellaview.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Sufami Turbo", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Sufami%20Turbo.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Nintendo DSi", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20DSi.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Wii Digital", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Wii%20%28Digital%29.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Wii U Digital", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Wii%20U%20%28Digital%29.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Sony PlayStation Portable", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Portable.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Sony PlayStation Portable PSN", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Portable%20%28PSN%29.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Sony PlayStation Portable PSX2PSP", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Portable%20%28PSX2PSP%29.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Sony PlayStation Vita", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Vita.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Sony PlayStation Vita PSN", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Vita%20%28PSN%29.dat"),
            new NoIntroCatalogSourceSeed("No-Intro Sony PlayStation 3 PSN", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%203%20%28PSN%29.dat")
        };

        public static PlatformFamily MapPlatformFamily(string systemKey)
        {
            return systemKey switch
            {
                "nintendo---game-boy" => PlatformFamily.NintendoGB,
                "nintendo---game-boy-color" => PlatformFamily.NintendoGBC,
                "nintendo---game-boy-advance" => PlatformFamily.NintendoGBA,
                "nintendo---game-boy-advance-multiboot" => PlatformFamily.NintendoGBA,
                "nintendo---game-boy-advance--multiboot" => PlatformFamily.NintendoGBA,
                "nintendo---game-boy-advance-e-reader" => PlatformFamily.NintendoGBA,
                "nintendo---game-boy-advance--e-reader" => PlatformFamily.NintendoGBA,
                "nintendo---game-boy-advance-play-yan" => PlatformFamily.NintendoGBA,
                "nintendo---game-boy-advance--play-yan" => PlatformFamily.NintendoGBA,
                "nintendo---game-boy-advance-video" => PlatformFamily.NintendoGBA,
                "nintendo---game-boy-advance--video" => PlatformFamily.NintendoGBA,
                "nintendo---nintendo-ds" => PlatformFamily.NintendoDS,
                "nintendo---nintendo-dsi" => PlatformFamily.NintendoDSi,
                "nintendo---nintendo-ds-download-play" => PlatformFamily.NintendoDS,
                "nintendo---nintendo-ds--download-play" => PlatformFamily.NintendoDS,
                "nintendo---nintendo-ds-dsvision-sd-cards" => PlatformFamily.NintendoDS,
                "nintendo---nintendo-ds--dsvision-sd-cards" => PlatformFamily.NintendoDS,
                "nintendo---nintendo-3ds" => PlatformFamily.Nintendo3DS,
                "nintendo---nintendo-3ds-digital" => PlatformFamily.Nintendo3DS,
                "nintendo---nintendo-3ds--digital" => PlatformFamily.Nintendo3DS,
                "nintendo---new-nintendo-3ds" => PlatformFamily.Nintendo3DS,
                "nintendo---new-nintendo-3ds-digital" => PlatformFamily.Nintendo3DS,
                "nintendo---new-nintendo-3ds--digital" => PlatformFamily.Nintendo3DS,
                "nintendo---nintendo-entertainment-system" => PlatformFamily.NintendoNES,
                "nintendo---super-nintendo-entertainment-system" => PlatformFamily.NintendoSNES,
                "nintendo---nintendo-64" => PlatformFamily.NintendoN64,
                "nintendo---nintendo-64dd" => PlatformFamily.NintendoN64,
                "nintendo---family-computer-disk-system" => PlatformFamily.NintendoFDS,
                "nintendo---virtual-boy" => PlatformFamily.NintendoVirtualBoy,
                "nintendo---pokemon-mini" => PlatformFamily.NintendoPokemonMini,
                "nintendo---satellaview" => PlatformFamily.NintendoSNES,
                "nintendo---sufami-turbo" => PlatformFamily.NintendoSNES,
                "nintendo---wii--digital" => PlatformFamily.NintendoWii,
                "nintendo---wii-u--digital" => PlatformFamily.NintendoWiiU,
                "sony---playstation-3--psn" => PlatformFamily.SonyPS3,
                "sony---playstation-portable" => PlatformFamily.SonyPSP,
                "sony---playstation-portable--psn" => PlatformFamily.SonyPSP,
                "sony---playstation-portable--psx2psp" => PlatformFamily.SonyPSP,
                "sony---playstation-vita" => PlatformFamily.SonyPSVita,
                "sony---playstation-vita--psn" => PlatformFamily.SonyPSVita,
                _ => PlatformFamily.Unknown
            };
        }

        public static bool MatchesGamePlatform(PlatformFamily gamePlatform, PlatformFamily catalogPlatform)
        {
            if (gamePlatform == catalogPlatform)
            {
                return true;
            }

            return (gamePlatform == PlatformFamily.Nintendo && GamePlatform.IsNintendoFamily(catalogPlatform)) ||
                (gamePlatform == PlatformFamily.PlayStation && GamePlatform.IsPlayStationFamily(catalogPlatform));
        }
    }

    public class NoIntroCatalogSourceSeed
    {
        public NoIntroCatalogSourceSeed(string name, string sourceUrl)
        {
            Name = name;
            SourceUrl = sourceUrl;
        }

        public string Name { get; }
        public string SourceUrl { get; }
    }
}
