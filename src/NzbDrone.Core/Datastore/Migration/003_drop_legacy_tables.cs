using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(3)]
    public class DropLegacyTables : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Table("Credits").IfExists();
            Delete.Table("SubtitleFiles").IfExists();
        }
    }
}
