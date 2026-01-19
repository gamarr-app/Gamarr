using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(133)]
    public class add_minimumavailability : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            if (!Schema.Table("NetImport").Column("MinimumAvailability").Exists())
            {
                Alter.Table("NetImport").AddColumn("MinimumAvailability").AsInt32().WithDefaultValue((int)GameStatusType.Released);
            }

            if (!Schema.Table("Games").Column("MinimumAvailability").Exists())
            {
                Alter.Table("Games").AddColumn("MinimumAvailability").AsInt32().WithDefaultValue((int)GameStatusType.Released);
            }
        }
    }
}
