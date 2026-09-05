using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(17)]
    public class add_platform_root_folders : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Default root folder per PlatformFamily, used to pre-fill (and, for
            // API/import-list adds, to supply) Games.RootFolderPath. One row per
            // platform; the Unknown row is the global default.
            Create.TableForModel("PlatformRootFolders")
                  .WithColumn("Platform").AsInt32().NotNullable().Unique()
                  .WithColumn("Path").AsString().NotNullable();
        }
    }
}
