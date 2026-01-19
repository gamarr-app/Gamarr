using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(110)]
    public class add_phyiscal_release : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Games").AddColumn("PhysicalRelease").AsDateTime().Nullable();
        }
    }
}
