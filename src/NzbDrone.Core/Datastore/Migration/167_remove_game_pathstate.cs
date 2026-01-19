using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(167)]
    public class remove_game_pathstate : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("PathState").FromTable("Games");

            Execute.Sql("DELETE FROM \"Config\" WHERE \"Key\" IN ('pathsdefaultstatic')");

            Alter.Table("GameFiles").AddColumn("OriginalFilePath").AsString().Nullable();

            // This is Ignored in mapping, should not be in DB
            Delete.Column("Path").FromTable("GameFiles");
        }
    }
}
