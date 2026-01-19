using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(192)]
    public class add_on_delete_to_notifications : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Rename.Column("OnDelete").OnTable("Notifications").To("OnGameDelete");
            Alter.Table("Notifications").AddColumn("OnGameFileDelete").AsBoolean().WithDefaultValue(false);
            Alter.Table("Notifications").AddColumn("OnGameFileDeleteForUpgrade").AsBoolean().WithDefaultValue(false);
        }
    }
}
