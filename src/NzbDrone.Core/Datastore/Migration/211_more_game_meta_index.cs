using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(211)]
    public class more_game_meta_index : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index("IX_AlternativeTitles_GameMetadataId").OnTable("AlternativeTitles").OnColumn("GameMetadataId");
            Create.Index("IX_Credits_GameMetadataId").OnTable("Credits").OnColumn("GameMetadataId");
        }
    }
}
