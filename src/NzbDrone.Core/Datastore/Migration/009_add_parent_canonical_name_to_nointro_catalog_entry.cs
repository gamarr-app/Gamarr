using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(9)]
    public class add_parent_canonical_name_to_nointro_catalog_entry : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NoIntroCatalogEntries")
                 .AddColumn("ParentCanonicalName")
                 .AsString()
                 .Nullable();
        }
    }
}
