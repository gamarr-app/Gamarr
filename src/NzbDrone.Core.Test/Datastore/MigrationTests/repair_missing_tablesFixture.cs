using Dapper;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.MigrationTests
{
    [TestFixture]
    public class repair_missing_tablesFixture : MigrationTest<repair_missing_tables>
    {
        [Test]
        public void should_recreate_platform_root_folders_when_it_is_missing()
        {
            // Reproduces Sentry DOTNET-ASPNETCORE-36: VersionInfo records
            // migration 17, so MigrateUp() will never run it again, yet the
            // table is gone. Dropping it immediately before 19 runs is the
            // only way to reach that state from here.
            var db = WithDapperMigrationTestDb(m =>
            {
                m.Execute.Sql(@"DROP TABLE ""PlatformRootFolders"";");
            });

            db.QuerySingle<int>(@"SELECT COUNT(*) FROM ""PlatformRootFolders""").Should().Be(0);

            db.Execute(@"INSERT INTO ""PlatformRootFolders"" (""Platform"", ""Path"") VALUES (1, '/games/switch')");

            db.QuerySingle<string>(@"SELECT ""Path"" FROM ""PlatformRootFolders""").Should().Be("/games/switch");
        }

        [Test]
        public void should_recreate_game_file_hashes_when_it_is_missing()
        {
            var db = WithDapperMigrationTestDb(m =>
            {
                m.Execute.Sql(@"DROP TABLE ""GameFileHashes"";");
            });

            db.QuerySingle<int>(@"SELECT COUNT(*) FROM ""GameFileHashes""").Should().Be(0);
        }

        [Test]
        public void should_preserve_existing_platform_root_folder_rows()
        {
            // The repair must be a no-op on a healthy database: it runs on
            // every install, almost none of which are broken.
            var db = WithDapperMigrationTestDb(m =>
            {
                m.Insert.IntoTable("PlatformRootFolders").Row(new
                {
                    Platform = 2,
                    Path = "/games/ps5"
                });
            });

            db.QuerySingle<int>(@"SELECT COUNT(*) FROM ""PlatformRootFolders""").Should().Be(1);
            db.QuerySingle<string>(@"SELECT ""Path"" FROM ""PlatformRootFolders""").Should().Be("/games/ps5");
        }
    }
}
