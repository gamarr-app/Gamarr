using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(209)]
    public class game_meta_collection_index : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index("IX_GameMetadata_CollectionIgdbId").OnTable("GameMetadata").OnColumn("CollectionIgdbId");
            Create.Index("IX_GameTranslations_GameMetadataId").OnTable("GameTranslations").OnColumn("GameMetadataId");
        }
    }
}
