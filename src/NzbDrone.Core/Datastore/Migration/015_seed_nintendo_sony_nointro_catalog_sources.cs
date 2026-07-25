using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(15)]
    public class seed_nintendo_sony_nointro_catalog_sources : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Nintendo Entertainment System", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20Entertainment%20System.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Super Nintendo Entertainment System", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Super%20Nintendo%20Entertainment%20System.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Nintendo 64", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%2064.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Nintendo 64DD", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%2064DD.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Family Computer Disk System", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Family%20Computer%20Disk%20System.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Virtual Boy", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Virtual%20Boy.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Pokemon Mini", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Pokemon%20Mini.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Satellaview", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Satellaview.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Sufami Turbo", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Sufami%20Turbo.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Nintendo DSi", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20DSi.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Wii Digital", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Wii%20%28Digital%29.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Wii U Digital", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Wii%20U%20%28Digital%29.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Sony PlayStation Portable", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Portable.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Sony PlayStation Portable PSN", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Portable%20%28PSN%29.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Sony PlayStation Portable PSX2PSP", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Portable%20%28PSX2PSP%29.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Sony PlayStation Vita", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Vita.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Sony PlayStation Vita PSN", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%20Vita%20%28PSN%29.dat" });
            Insert.IntoTable("NoIntroCatalogSources").Row(new { Name = "No-Intro Sony PlayStation 3 PSN", SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Sony%20-%20PlayStation%203%20%28PSN%29.dat" });
        }
    }
}
