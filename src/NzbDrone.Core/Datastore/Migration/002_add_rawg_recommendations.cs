using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(2)]
    public class AddRawgRecommendations : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Rename Recommendations to IgdbRecommendations for clarity
            Rename.Column("Recommendations").OnTable("GameMetadata").To("IgdbRecommendations");

            // Add new column for RAWG recommendations
            Alter.Table("GameMetadata")
                .AddColumn("RawgRecommendations").AsString().Nullable().WithDefaultValue("[]");
        }
    }
}
