using System;
using Dapper;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.MigrationTests
{
    [TestFixture]
    public class import_exclusions_allow_steam_onlyFixture : MigrationTest<import_exclusions_allow_steam_only>
    {
        [Test]
        public void should_allow_multiple_steam_only_exclusions()
        {
            var db = WithDapperMigrationTestDb(m =>
            {
                m.Insert.IntoTable("ImportExclusions").Row(new
                {
                    IgdbId = 0,
                    GameTitle = "First Steam Game",
                    GameYear = 2020,
                    SteamAppId = 100
                });
            });

            db.Execute("INSERT INTO \"ImportExclusions\" (\"IgdbId\", \"GameTitle\", \"GameYear\", \"SteamAppId\") VALUES (0, 'Second Steam Game', 2021, 200)");

            db.QuerySingle<int>("SELECT COUNT(*) FROM \"ImportExclusions\" WHERE \"IgdbId\" = 0").Should().Be(2);
        }

        [Test]
        public void should_preserve_rows_and_still_reject_duplicate_igdb_ids()
        {
            var db = WithDapperMigrationTestDb(m =>
            {
                m.Insert.IntoTable("ImportExclusions").Row(new
                {
                    IgdbId = 42,
                    GameTitle = "Igdb Game",
                    GameYear = 2019,
                    SteamAppId = 0
                });
            });

            db.QuerySingle<int>("SELECT COUNT(*) FROM \"ImportExclusions\"").Should().Be(1);

            Action duplicate = () => db.Execute("INSERT INTO \"ImportExclusions\" (\"IgdbId\", \"GameTitle\", \"GameYear\", \"SteamAppId\") VALUES (42, 'Duplicate', 2019, 0)");

            duplicate.Should().Throw<Exception>().Which.Message.Should().Contain("UNIQUE");
        }
    }
}
