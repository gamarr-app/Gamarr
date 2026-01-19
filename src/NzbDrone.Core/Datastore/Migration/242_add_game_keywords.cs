using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(242)]
    public class add_game_keywords : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("GameMetadata").AddColumn("Keywords").AsString().Nullable();
        }
    }
}
