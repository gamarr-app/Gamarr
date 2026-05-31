using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(3)]
    public class add_game_file_version : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // GameFile.GameVersion was on the model but had no column and didn't
            // implement IEmbeddedDocument, so the parsed install version was
            // silently dropped on persistence. Add the column (serialized JSON,
            // like Quality) so version-upgrade decisions have a real baseline.
            Alter.Table("GameFiles")
                 .AddColumn("GameVersion").AsString().Nullable();
        }
    }
}
