using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(245)]
    public class remove_igdbid_unique : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // SQLite: Need to recreate table to remove inline UNIQUE constraint on IgdbId
            // This allows multiple Steam games to have IgdbId=0
            IfDatabase("sqlite").Execute.Sql(@"
                -- Create new table without UNIQUE on IgdbId
                CREATE TABLE ""GameMetadata_new"" (
                    ""Id"" INTEGER NOT NULL UNIQUE PRIMARY KEY AUTOINCREMENT,
                    ""IgdbId"" INTEGER NOT NULL,
                    ""ImdbId"" TEXT,
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

                -- Copy data
                INSERT INTO ""GameMetadata_new"" SELECT * FROM ""GameMetadata"";

                -- Drop old table and indexes
                DROP TABLE ""GameMetadata"";

                -- Rename new table
                ALTER TABLE ""GameMetadata_new"" RENAME TO ""GameMetadata"";

                -- Recreate indexes (non-unique for IgdbId)
                CREATE INDEX ""IX_GameMetadata_IgdbId"" ON ""GameMetadata"" (""IgdbId"" ASC);
                CREATE INDEX ""IX_GameMetadata_CleanTitle"" ON ""GameMetadata"" (""CleanTitle"" ASC);
                CREATE INDEX ""IX_GameMetadata_CleanOriginalTitle"" ON ""GameMetadata"" (""CleanOriginalTitle"" ASC);
                CREATE INDEX ""IX_GameMetadata_CollectionIgdbId"" ON ""GameMetadata"" (""CollectionIgdbId"" ASC);
                CREATE INDEX ""IX_GameMetadata_SteamAppId"" ON ""GameMetadata"" (""SteamAppId"" ASC);
                CREATE INDEX ""IX_GameMetadata_RawgId"" ON ""GameMetadata"" (""RawgId"" ASC);
            ");

            // PostgreSQL: Can alter constraint directly
            IfDatabase("postgres").Execute.Sql(@"
                ALTER TABLE ""GameMetadata"" DROP CONSTRAINT IF EXISTS ""IX_GameMetadata_IgdbId"";
                DROP INDEX IF EXISTS ""IX_GameMetadata_IgdbId"";
                CREATE INDEX ""IX_GameMetadata_IgdbId"" ON ""GameMetadata"" (""IgdbId"" ASC);
            ");
        }
    }
}
