using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(136)]
    public class add_pathstate_to_games : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Games").AddColumn("PathState").AsInt32().WithDefaultValue(2);
        }
    }
}
