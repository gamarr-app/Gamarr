using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(2)]
    public class add_stall_timeout_hours : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Per-client stall-timeout knob backing the StalledDownload
            // detection in FailedDownloadService.CheckForStall. Default 0
            // keeps the existing behavior (no stall check); users opt in
            // by setting a number of hours.
            Alter.Table("DownloadClients")
                 .AddColumn("StallTimeoutHours").AsInt32().WithDefaultValue(0);
        }
    }
}
