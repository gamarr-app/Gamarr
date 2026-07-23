using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(14)]
    public class seed_3ds_nointro_catalog_sources : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo 3DS",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%203DS.dat"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo 3DS Digital",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%203DS%20%28Digital%29.dat"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro New Nintendo 3DS",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20New%20Nintendo%203DS.dat"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro New Nintendo 3DS Digital",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20New%20Nintendo%203DS%20%28Digital%29.dat"
            });
        }
    }
}
