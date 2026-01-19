using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class remove_predbFixture : MigrationTest<remove_predb>
    {
        [Test]
        public void should_change_min_avail_from_predb_on_list()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("ImportLists").Row(new
                {
                    Enabled = true,
                    EnableAuto = true,
                    RootFolderPath = "D:\\Games",
                    ProfileId = 1,
                    MinimumAvailability = 4,
                    ShouldMonitor = true,
                    Name = "IMDB List",
                    Implementation = "GamarrLists",
                    Settings = new GamarrListSettings169
                    {
                        APIURL = "https://api.gamarr.video/v2",
                        Path = "/imdb/list?listId=ls000199717",
                    }.ToJson(),
                    ConfigContract = "GamarrSettings"
                });
            });

            var items = db.Query<ListDefinition201>("SELECT \"Id\", \"MinimumAvailability\" FROM \"ImportLists\"");

            items.Should().HaveCount(1);
            items.First().MinimumAvailability.Should().Be(3);
        }

        [Test]
        public void should_change_min_avail_from_predb_on_game()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Games").Row(new
                {
                    Monitored = true,
                    Title = "Title",
                    CleanTitle = "CleanTitle",
                    Status = 3,
                    MinimumAvailability = 4,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    HasPreDBEntry = false,
                    Runtime = 90,
                    OriginalLanguage = 1,
                    ProfileId = 1,
                    GameFileId = 0,
                    Path = string.Format("/Games/{0}", "Title"),
                    TitleSlug = 123456,
                    IgdbId = 132456,
                    Added = DateTime.UtcNow,
                    LastInfoSync = DateTime.UtcNow,
                });
            });

            var items = db.Query<Game201>("SELECT \"Id\", \"MinimumAvailability\" FROM \"Games\"");

            items.Should().HaveCount(1);
            items.First().MinimumAvailability.Should().Be(3);
        }
    }

    public class ListDefinition201
    {
        public int Id { get; set; }
        public int MinimumAvailability { get; set; }
    }

    public class Game201
    {
        public int Id { get; set; }
        public int MinimumAvailability { get; set; }
    }

    public class GamarrListSettings169
    {
        public string APIURL { get; set; }
        public string Path { get; set; }
    }
}
