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
    public class collectionsFixture : MigrationTest<add_collections>
    {
        [Test]
        public void should_add_collection_from_game_and_link_back_to_game()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Games").Row(new
                {
                    Monitored = true,
                    MinimumAvailability = 4,
                    ProfileId = 1,
                    GameFileId = 0,
                    GameMetadataId = 1,
                    Path = string.Format("/Games/{0}", "Title"),
                });

                c.Insert.IntoTable("GameMetadata").Row(new
                {
                    Title = "Title",
                    CleanTitle = "CleanTitle",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    Runtime = 90,
                    OriginalTitle = "Title",
                    CleanOriginalTitle = "CleanTitle",
                    OriginalLanguage = 1,
                    IgdbId = 132456,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });
            });

            var collections = db.Query<Collection208>("SELECT \"Id\", \"Title\", \"IgdbId\", \"Monitored\" FROM \"Collections\"");

            collections.Should().HaveCount(1);
            collections.First().IgdbId.Should().Be(11);
            collections.First().Title.Should().Be("Some Collection");
            collections.First().Monitored.Should().BeFalse();

            var games = db.Query<Game208>("SELECT \"Id\", \"CollectionIgdbId\" FROM \"GameMetadata\"");

            games.Should().HaveCount(1);
            games.First().CollectionIgdbId.Should().Be(collections.First().IgdbId);
        }

        [Test]
        public void should_skip_collection_from_game_without_name()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Games").Row(new
                {
                    Monitored = true,
                    MinimumAvailability = 4,
                    ProfileId = 1,
                    GameFileId = 0,
                    GameMetadataId = 1,
                    Path = string.Format("/Games/{0}", "Title"),
                });

                c.Insert.IntoTable("GameMetadata").Row(new
                {
                    Title = "Title",
                    CleanTitle = "CleanTitle",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    Runtime = 90,
                    OriginalTitle = "Title",
                    CleanOriginalTitle = "CleanTitle",
                    OriginalLanguage = 1,
                    IgdbId = 132456,
                    Collection = new { IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });
            });

            var collections = db.Query<Collection208>("SELECT \"Id\", \"Title\", \"IgdbId\", \"Monitored\" FROM \"Collections\"");

            collections.Should().HaveCount(1);
            collections.First().IgdbId.Should().Be(11);
            collections.First().Title.Should().Be("Collection 11");
            collections.First().Monitored.Should().BeFalse();

            var games = db.Query<Game208>("SELECT \"Id\", \"CollectionIgdbId\" FROM \"GameMetadata\"");

            games.Should().HaveCount(1);
            games.First().CollectionIgdbId.Should().Be(collections.First().IgdbId);
        }

        [Test]
        public void should_not_duplicate_collection()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Games").Row(new
                {
                    Monitored = true,
                    MinimumAvailability = 4,
                    ProfileId = 1,
                    GameFileId = 0,
                    GameMetadataId = 1,
                    Path = string.Format("/Games/{0}", "Title"),
                });

                c.Insert.IntoTable("GameMetadata").Row(new
                {
                    Title = "Title",
                    CleanTitle = "CleanTitle",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    Runtime = 90,
                    OriginalTitle = "Title",
                    CleanOriginalTitle = "CleanTitle",
                    OriginalLanguage = 1,
                    IgdbId = 132456,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });

                c.Insert.IntoTable("Games").Row(new
                {
                    Monitored = true,
                    MinimumAvailability = 4,
                    ProfileId = 1,
                    GameFileId = 0,
                    GameMetadataId = 2,
                    Path = string.Format("/Games/{0}", "Title"),
                });

                c.Insert.IntoTable("GameMetadata").Row(new
                {
                    Title = "Title2",
                    CleanTitle = "CleanTitle2",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    Runtime = 90,
                    OriginalTitle = "Title2",
                    CleanOriginalTitle = "CleanTitle2",
                    OriginalLanguage = 1,
                    IgdbId = 132457,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });
            });

            var collections = db.Query<Collection208>("SELECT \"Id\", \"Title\", \"IgdbId\", \"Monitored\" FROM \"Collections\"");

            collections.Should().HaveCount(1);
            collections.First().IgdbId.Should().Be(11);
            collections.First().Title.Should().Be("Some Collection");
            collections.First().Monitored.Should().BeFalse();
        }

        [Test]
        public void should_migrate_true_monitor_setting_on_lists()
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

            var items = db.Query<ListDefinition208>("SELECT \"Id\", \"Monitor\" FROM \"ImportLists\"");

            items.Should().HaveCount(1);
            items.First().Monitor.Should().Be(0);
        }

        [Test]
        public void should_migrate_false_monitor_setting_on_lists()
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
                    ShouldMonitor = false,
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

            var items = db.Query<ListDefinition208>("SELECT \"Id\", \"Monitor\" FROM \"ImportLists\"");

            items.Should().HaveCount(1);
            items.First().Monitor.Should().Be(2);
        }

        [Test]
        public void should_purge_igdb_collection_lists()
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
                    ShouldMonitor = false,
                    Name = "IMDB List",
                    Implementation = "TMDbCollectionImport",
                    Settings = new IgdbCollectionListSettings207
                    {
                        CollectionId = "11"
                    }.ToJson(),
                    ConfigContract = "TMDbCollectionSettings"
                });
            });

            var items = db.Query<ListDefinition208>("SELECT \"Id\", \"Monitor\" FROM \"ImportLists\"");

            items.Should().HaveCount(0);
        }

        [Test]
        public void should_monitor_new_collection_if_list_enabled_and_auto()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Games").Row(new
                {
                    Monitored = true,
                    MinimumAvailability = 4,
                    ProfileId = 1,
                    GameFileId = 0,
                    GameMetadataId = 1,
                    Path = string.Format("/Games/{0}", "Title"),
                });

                c.Insert.IntoTable("GameMetadata").Row(new
                {
                    Title = "Title",
                    CleanTitle = "CleanTitle",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    Runtime = 90,
                    OriginalTitle = "Title",
                    CleanOriginalTitle = "CleanTitle",
                    OriginalLanguage = 1,
                    IgdbId = 132456,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });

                c.Insert.IntoTable("ImportLists").Row(new
                {
                    Enabled = true,
                    EnableAuto = true,
                    RootFolderPath = "D:\\Games",
                    ProfileId = 1,
                    MinimumAvailability = 4,
                    ShouldMonitor = false,
                    Name = "IMDB List",
                    Implementation = "TMDbCollectionImport",
                    Settings = new IgdbCollectionListSettings207
                    {
                        CollectionId = "11"
                    }.ToJson(),
                    ConfigContract = "TMDbCollectionSettings"
                });
            });

            var items = db.Query<Collection208>("SELECT \"Id\", \"Monitored\" FROM \"Collections\"");

            items.Should().HaveCount(1);
            items.First().Monitored.Should().BeTrue();
        }
    }

    public class Collection208
    {
        public int Id { get; set; }
        public int IgdbId { get; set; }
        public string Title { get; set; }
        public bool Monitored { get; set; }
    }

    public class Game208
    {
        public int Id { get; set; }
        public int CollectionIgdbId { get; set; }
    }

    public class ListDefinition208
    {
        public int Id { get; set; }
        public int Monitor { get; set; }
    }

    public class IgdbCollectionListSettings207
    {
        public string CollectionId { get; set; }
    }
}
