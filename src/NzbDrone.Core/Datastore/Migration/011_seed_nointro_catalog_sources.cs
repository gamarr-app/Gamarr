using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(11)]
    public class seed_nointro_catalog_sources : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo Game Boy",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy.dat"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo Game Boy Color",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy%20Color.dat"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo Game Boy Advance",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy%20Advance.dat"
            });

            Insert.IntoTable("NoIntroCatalogSources").Row(new
            {
                Name = "No-Intro Nintendo DS",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20DS.dat"
            });
        }
    }
}
