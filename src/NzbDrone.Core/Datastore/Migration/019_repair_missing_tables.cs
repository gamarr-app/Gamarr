using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Repair, not a schema change. Sentry DOTNET-ASPNETCORE-36 is a
    // `no such table: PlatformRootFolders` on GET /api/v3/platformrootfolder
    // from an install whose VersionInfo had already recorded migration 17 —
    // so MigrateUp() will never run 17 again on that database, and the table
    // stays missing for the lifetime of the install. 18's GameFileHashes has
    // exactly the same exposure (added the same way, read by the components
    // GET), so both are repaired here.
    //
    // CREATE TABLE IF NOT EXISTS makes this a no-op on every healthy
    // database, which is the only safe shape for a migration that exists to
    // undo an unknown amount of damage: we cannot tell from here which of the
    // two tables a given install is missing.
    //
    // Column types and constraints are copied verbatim from 017/018 — a
    // repaired table has to be indistinguishable from a correctly migrated
    // one, or the next migration that touches it will behave differently per
    // install.
    [Migration(19)]
    public class repair_missing_tables : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            IfDatabase("sqlite").Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS ""PlatformRootFolders"" (
                    ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ""Platform"" INTEGER NOT NULL UNIQUE,
                    ""Path"" TEXT NOT NULL);");

            IfDatabase("sqlite").Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS ""GameFileHashes"" (
                    ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ""GameFileId"" INTEGER NOT NULL UNIQUE,
                    ""Size"" INTEGER NOT NULL,
                    ""LastModifiedUtcTicks"" INTEGER NOT NULL,
                    ""Md5"" TEXT,
                    ""Sha1"" TEXT,
                    ""Crc32"" TEXT,
                    ""HashedAt"" DATETIME NOT NULL);");

            IfDatabase("postgresql").Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS ""PlatformRootFolders"" (
                    ""Id"" SERIAL NOT NULL PRIMARY KEY,
                    ""Platform"" INTEGER NOT NULL UNIQUE,
                    ""Path"" TEXT NOT NULL);");

            IfDatabase("postgresql").Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS ""GameFileHashes"" (
                    ""Id"" SERIAL NOT NULL PRIMARY KEY,
                    ""GameFileId"" INTEGER NOT NULL UNIQUE,
                    ""Size"" BIGINT NOT NULL,
                    ""LastModifiedUtcTicks"" BIGINT NOT NULL,
                    ""Md5"" TEXT,
                    ""Sha1"" TEXT,
                    ""Crc32"" TEXT,
                    ""HashedAt"" TIMESTAMP NOT NULL);");
        }
    }
}
