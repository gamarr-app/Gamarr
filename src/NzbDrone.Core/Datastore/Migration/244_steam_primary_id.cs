using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(244)]
    public class steam_primary_id : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Add RawgId as secondary identifier
            Alter.Table("GameMetadata").AddColumn("RawgId").AsInt32().WithDefaultValue(0);

            // Create index on SteamAppId for faster lookups (primary identifier)
            Create.Index("IX_GameMetadata_SteamAppId").OnTable("GameMetadata")
                .OnColumn("SteamAppId").Ascending();

            // Create index on RawgId for lookups (secondary identifier)
            Create.Index("IX_GameMetadata_RawgId").OnTable("GameMetadata")
                .OnColumn("RawgId").Ascending();

            // Add SteamAppId to ImportExclusions for exclusion by Steam ID
            Alter.Table("ImportExclusions").AddColumn("SteamAppId").AsInt32().WithDefaultValue(0);
        }
    }
}
