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

            // Note: the DAT-o-MATIC-only niche systems (GBA Multiboot/e-Reader/
            // Play-Yan/Video, DSvision) are intentionally NOT seeded — the
            // datomatic:// scrape path is currently non-functional, so seeding
            // them would surface permanently-failing sources. The support code
            // remains; re-seed once FetchDatOMaticNumbered is hardened.
        }
    }
}
