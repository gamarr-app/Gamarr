using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(2)]
    public class AddRawgRecommendations : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Add RawgRecommendations column if it doesn't exist
            // Note: IgdbRecommendations is already created in 001_initial_setup
            // Only add RawgRecommendations if not already present
            if (!Schema.Table("GameMetadata").Column("RawgRecommendations").Exists())
            {
                Alter.Table("GameMetadata")
                    .AddColumn("RawgRecommendations").AsString().Nullable().WithDefaultValue("[]");
            }
        }
    }
}
