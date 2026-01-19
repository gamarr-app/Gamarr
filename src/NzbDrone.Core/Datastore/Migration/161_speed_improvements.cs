using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(161)]
    public class speed_improvements : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Auto indices SQLite is creating
            Create.Index("IX_GameFiles_GameId").OnTable("GameFiles").OnColumn("GameId");
            Create.Index("IX_AlternativeTitles_GameId").OnTable("AlternativeTitles").OnColumn("GameId");

            // Speed up release processing (these are present in Sonarr)
            Create.Index("IX_Games_CleanTitle").OnTable("Games").OnColumn("CleanTitle");
            Create.Index("IX_Games_ImdbId").OnTable("Games").OnColumn("ImdbId");
            Create.Index("IX_Games_IgdbId").OnTable("Games").OnColumn("IgdbId");
        }
    }
}
