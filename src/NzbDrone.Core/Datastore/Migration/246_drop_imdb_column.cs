using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(246)]
    public class drop_imdb_column : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // SQLite: Need to recreate table to drop column
            IfDatabase("sqlite").Execute.Sql(@"
                -- Create new table without ImdbId
                CREATE TABLE ""GameMetadata_new"" (
                    ""Id"" INTEGER NOT NULL UNIQUE PRIMARY KEY AUTOINCREMENT,
                    ""IgdbId"" INTEGER NOT NULL,
                    ""Images"" TEXT NOT NULL,
                    ""Genres"" TEXT,
                    ""Title"" TEXT NOT NULL,
                    ""SortTitle"" TEXT,
                    ""CleanTitle"" TEXT,
                    ""OriginalTitle"" TEXT,
                    ""CleanOriginalTitle"" TEXT,
                    ""OriginalLanguage"" INTEGER NOT NULL,
                    ""Status"" INTEGER NOT NULL,
                    ""LastInfoSync"" DATETIME,
                    ""Runtime"" INTEGER NOT NULL,
                    ""EarlyAccess"" DATETIME,
                    ""PhysicalRelease"" DATETIME,
                    ""DigitalRelease"" DATETIME,
                    ""Year"" INTEGER,
                    ""SecondaryYear"" INTEGER,
                    ""Ratings"" TEXT,
                    ""Recommendations"" TEXT NOT NULL,
                    ""Certification"" TEXT,
                    ""YouTubeTrailerId"" TEXT,
                    ""Studio"" TEXT,
                    ""Overview"" TEXT,
                    ""Website"" TEXT,
                    ""Popularity"" NUMERIC,
                    ""CollectionIgdbId"" INTEGER,
                    ""CollectionTitle"" TEXT,
                    ""Keywords"" TEXT,
                    ""SteamAppId"" INTEGER NOT NULL DEFAULT 0,
                    ""GameType"" INTEGER NOT NULL DEFAULT 0,
                    ""ParentGameId"" INTEGER,
                    ""DlcIds"" TEXT,
                    ""Platforms"" TEXT,
                    ""GameModes"" TEXT,
                    ""Themes"" TEXT,
                    ""Developer"" TEXT,
                    ""Publisher"" TEXT,
                    ""GameEngine"" TEXT,
                    ""AggregatedRating"" NUMERIC,
                    ""AggregatedRatingCount"" INTEGER,
                    ""RawgId"" INTEGER NOT NULL DEFAULT 0
                );

                -- Copy data (excluding ImdbId)
                INSERT INTO ""GameMetadata_new"" (
                    ""Id"", ""IgdbId"", ""Images"", ""Genres"", ""Title"", ""SortTitle"", ""CleanTitle"",
                    ""OriginalTitle"", ""CleanOriginalTitle"", ""OriginalLanguage"", ""Status"", ""LastInfoSync"",
                    ""Runtime"", ""EarlyAccess"", ""PhysicalRelease"", ""DigitalRelease"", ""Year"", ""SecondaryYear"",
                    ""Ratings"", ""Recommendations"", ""Certification"", ""YouTubeTrailerId"", ""Studio"", ""Overview"",
                    ""Website"", ""Popularity"", ""CollectionIgdbId"", ""CollectionTitle"", ""Keywords"", ""SteamAppId"",
                    ""GameType"", ""ParentGameId"", ""DlcIds"", ""Platforms"", ""GameModes"", ""Themes"", ""Developer"",
                    ""Publisher"", ""GameEngine"", ""AggregatedRating"", ""AggregatedRatingCount"", ""RawgId""
                )
                SELECT
                    ""Id"", ""IgdbId"", ""Images"", ""Genres"", ""Title"", ""SortTitle"", ""CleanTitle"",
                    ""OriginalTitle"", ""CleanOriginalTitle"", ""OriginalLanguage"", ""Status"", ""LastInfoSync"",
                    ""Runtime"", ""EarlyAccess"", ""PhysicalRelease"", ""DigitalRelease"", ""Year"", ""SecondaryYear"",
                    ""Ratings"", ""Recommendations"", ""Certification"", ""YouTubeTrailerId"", ""Studio"", ""Overview"",
                    ""Website"", ""Popularity"", ""CollectionIgdbId"", ""CollectionTitle"", ""Keywords"", ""SteamAppId"",
                    ""GameType"", ""ParentGameId"", ""DlcIds"", ""Platforms"", ""GameModes"", ""Themes"", ""Developer"",
                    ""Publisher"", ""GameEngine"", ""AggregatedRating"", ""AggregatedRatingCount"", ""RawgId""
                FROM ""GameMetadata"";

                -- Drop old table
                DROP TABLE ""GameMetadata"";

                -- Rename new table
                ALTER TABLE ""GameMetadata_new"" RENAME TO ""GameMetadata"";

                -- Recreate indexes
                CREATE INDEX ""IX_GameMetadata_IgdbId"" ON ""GameMetadata"" (""IgdbId"" ASC);
                CREATE INDEX ""IX_GameMetadata_CleanTitle"" ON ""GameMetadata"" (""CleanTitle"" ASC);
                CREATE INDEX ""IX_GameMetadata_CleanOriginalTitle"" ON ""GameMetadata"" (""CleanOriginalTitle"" ASC);
                CREATE INDEX ""IX_GameMetadata_CollectionIgdbId"" ON ""GameMetadata"" (""CollectionIgdbId"" ASC);
                CREATE INDEX ""IX_GameMetadata_SteamAppId"" ON ""GameMetadata"" (""SteamAppId"" ASC);
                CREATE INDEX ""IX_GameMetadata_RawgId"" ON ""GameMetadata"" (""RawgId"" ASC);
            ");

            // PostgreSQL: Can drop column directly
            IfDatabase("postgres").Execute.Sql(@"
                ALTER TABLE ""GameMetadata"" DROP COLUMN IF EXISTS ""ImdbId"";
            ");
        }
    }
}
