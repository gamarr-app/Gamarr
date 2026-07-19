using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(12)]
    public class add_numbered_nointro_filenames : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NoIntroCatalogEntries")
                 .AddColumn("ReleaseNumber").AsString().Nullable()
                 .AddColumn("NumberedCanonicalFileName").AsString().Nullable();
        }
    }
}
