using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(4)]
    public class add_monitor_updates : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Per-game opt-in/out for automatically grabbing newer game
            // versions (updates/patches). Defaults to true so existing
            // libraries keep the current behavior driven by the global
            // UpgradeGameVersions toggle.
            Alter.Table("Games")
                 .AddColumn("MonitorUpdates").AsBoolean().NotNullable().WithDefaultValue(true);
        }
    }
}
