using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Versions 008-015 are reserved by the in-flight No-Intro catalog work
    // (PR #153); numbered 16 so both can merge without renumbering.
    [Migration(16)]
    public class import_exclusions_allow_steam_only : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // IgdbId is 0 for Steam-only games, so UNIQUE(IgdbId) allows at
            // most one Steam-only exclusion ever (Sentry 7624452260: the
            // second such exclusion crashed the delete handler). Rebuild
            // without the inline constraint and enforce uniqueness only for
            // real IGDB ids via a partial index.
            IfDatabase("sqlite").Execute.Sql(@"
                CREATE TABLE ""ImportExclusions_temp"" (
                    ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ""IgdbId"" INTEGER NOT NULL,
                    ""GameTitle"" TEXT,
                    ""GameYear"" INTEGER DEFAULT 0,
                    ""SteamAppId"" INTEGER NOT NULL DEFAULT 0);
                INSERT INTO ""ImportExclusions_temp"" (""Id"", ""IgdbId"", ""GameTitle"", ""GameYear"", ""SteamAppId"")
                    SELECT ""Id"", ""IgdbId"", ""GameTitle"", ""GameYear"", ""SteamAppId"" FROM ""ImportExclusions"";
                DROP TABLE ""ImportExclusions"";
                ALTER TABLE ""ImportExclusions_temp"" RENAME TO ""ImportExclusions"";
                CREATE UNIQUE INDEX ""IX_ImportExclusions_IgdbId"" ON ""ImportExclusions"" (""IgdbId"") WHERE ""IgdbId"" > 0;");

            IfDatabase("postgres").Execute.Sql(@"
                ALTER TABLE ""ImportExclusions"" DROP CONSTRAINT IF EXISTS ""UC_ImportExclusions_IgdbId"";
                DROP INDEX IF EXISTS ""IX_ImportExclusions_IgdbId"";
                CREATE UNIQUE INDEX ""IX_ImportExclusions_IgdbId"" ON ""ImportExclusions"" (""IgdbId"") WHERE ""IgdbId"" > 0;");
        }
    }
}
