using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(13)]
    public class seed_additional_nointro_catalog_sources : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo DS Download Play",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20DS%20%28Download%20Play%29.dat"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo DS DSvision SD Cards",
                SourceUrl = "datomatic://system/319"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo Game Boy Advance Multiboot",
                SourceUrl = "datomatic://system/137"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo Game Boy Advance e-Reader",
                SourceUrl = "datomatic://system/41"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo Game Boy Advance Play-Yan",
                SourceUrl = "datomatic://system/148"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo Game Boy Advance Video",
                SourceUrl = "datomatic://system/297"
            });
        }
    }
}
