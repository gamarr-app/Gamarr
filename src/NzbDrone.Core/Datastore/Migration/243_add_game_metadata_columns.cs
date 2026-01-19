using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(243)]
    public class add_game_metadata_columns : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Add game-specific columns to GameMetadata table
            // Note: EarlyAccess already exists in the base schema
            Alter.Table("GameMetadata").AddColumn("SteamAppId").AsInt32().WithDefaultValue(0);
            Alter.Table("GameMetadata").AddColumn("GameType").AsInt32().WithDefaultValue(0);
            Alter.Table("GameMetadata").AddColumn("ParentGameId").AsInt32().Nullable();
            Alter.Table("GameMetadata").AddColumn("DlcIds").AsString().Nullable();
            Alter.Table("GameMetadata").AddColumn("Platforms").AsString().Nullable();
            Alter.Table("GameMetadata").AddColumn("GameModes").AsString().Nullable();
            Alter.Table("GameMetadata").AddColumn("Themes").AsString().Nullable();
            Alter.Table("GameMetadata").AddColumn("Developer").AsString().Nullable();
            Alter.Table("GameMetadata").AddColumn("Publisher").AsString().Nullable();
            Alter.Table("GameMetadata").AddColumn("GameEngine").AsString().Nullable();
            Alter.Table("GameMetadata").AddColumn("AggregatedRating").AsDouble().Nullable();
            Alter.Table("GameMetadata").AddColumn("AggregatedRatingCount").AsInt32().Nullable();
        }
    }
}
