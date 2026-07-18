using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(5)]
    public class add_game_platform : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Per-entry platform (PlatformFamily enum; 0 = Unknown/any). Lets
            // the same title be added once per platform with its own root
            // folder and release filtering (gamarr-app/Gamarr#150).
            Alter.Table("Games")
                 .AddColumn("Platform").AsInt32().WithDefaultValue(0);
        }
    }
}
