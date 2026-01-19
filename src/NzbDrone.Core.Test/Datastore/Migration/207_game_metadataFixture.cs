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
    public class game_metadataFixture : MigrationTest<game_metadata>
    {
        [Test]
        public void should_add_metadata_from_game_and_link_back_to_game()
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
                    Runtime = 90,
                    OriginalLanguage = 1,
                    ProfileId = 1,
                    GameFileId = 0,
                    Path = string.Format("/Games/{0}", "Title"),
                    TitleSlug = 123456,
                    IgdbId = 132456,
                    Added = DateTime.UtcNow,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });
            });

            var metadata = db.Query<GameMetadata207>("SELECT \"Id\", \"Title\", \"IgdbId\" FROM \"GameMetadata\"");

            metadata.Should().HaveCount(1);
            metadata.First().IgdbId.Should().Be(132456);
            metadata.First().Title.Should().Be("Title");

            var games = db.Query<Game207>("SELECT \"Id\", \"GameMetadataId\" FROM \"Games\"");

            games.Should().HaveCount(1);
            games.First().GameMetadataId.Should().Be(metadata.First().Id);
        }

        [Test]
        public void should_link_metadata_to_credits()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Games").Row(new
                {
                    Id = 5,
                    Monitored = true,
                    Title = "Title",
                    CleanTitle = "CleanTitle",
                    Status = 3,
                    MinimumAvailability = 4,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    Runtime = 90,
                    OriginalLanguage = 1,
                    ProfileId = 1,
                    GameFileId = 0,
                    Path = string.Format("/Games/{0}", "Title"),
                    TitleSlug = 123456,
                    IgdbId = 132456,
                    Added = DateTime.UtcNow,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });

                c.Insert.IntoTable("Credits").Row(new
                {
                    GameId = 5,
                    CreditIgdbId = 123,
                    PersonIgdbId = 456,
                    Order = 1,
                    Type = 1,
                    Name = "Some Person",
                    Images = new[] { new { CoverType = "Poster" } }.ToJson()
                });
            });

            var metadata = db.Query<GameMetadata207>("SELECT \"Id\", \"Title\", \"IgdbId\" FROM \"GameMetadata\"");

            metadata.Should().HaveCount(1);
            metadata.First().IgdbId.Should().Be(132456);
            metadata.First().Title.Should().Be("Title");

            var games = db.Query<Game207>("SELECT \"Id\", \"GameMetadataId\" FROM \"Credits\"");

            games.Should().HaveCount(1);
            games.First().GameMetadataId.Should().Be(metadata.First().Id);
        }

        [Test]
        public void should_link_metadata_to_alt_title()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Games").Row(new
                {
                    Id = 5,
                    Monitored = true,
                    Title = "Title",
                    CleanTitle = "CleanTitle",
                    Status = 3,
                    MinimumAvailability = 4,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    Runtime = 90,
                    OriginalLanguage = 1,
                    ProfileId = 1,
                    GameFileId = 0,
                    Path = string.Format("/Games/{0}", "Title"),
                    TitleSlug = 123456,
                    IgdbId = 132456,
                    Added = DateTime.UtcNow,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });

                c.Insert.IntoTable("AlternativeTitles").Row(new
                {
                    GameId = 5,
                    Title = "Some Alt",
                    CleanTitle = "somealt",
                    SourceType = 1,
                    SourceId = 1,
                    Votes = 0,
                    VoteCount = 0,
                    Language = 1
                });
            });

            var metadata = db.Query<GameMetadata207>("SELECT \"Id\", \"Title\", \"IgdbId\" FROM \"GameMetadata\"");

            metadata.Should().HaveCount(1);
            metadata.First().IgdbId.Should().Be(132456);
            metadata.First().Title.Should().Be("Title");

            var games = db.Query<Game207>("SELECT \"Id\", \"GameMetadataId\" FROM \"AlternativeTitles\"");

            games.Should().HaveCount(1);
            games.First().GameMetadataId.Should().Be(metadata.First().Id);
        }

        [Test]
        public void should_link_metadata_to_translation()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Games").Row(new
                {
                    Id = 5,
                    Monitored = true,
                    Title = "Title",
                    CleanTitle = "CleanTitle",
                    Status = 3,
                    MinimumAvailability = 4,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Recommendations = new[] { 1 }.ToJson(),
                    Runtime = 90,
                    OriginalLanguage = 1,
                    ProfileId = 1,
                    GameFileId = 0,
                    Path = string.Format("/Games/{0}", "Title"),
                    TitleSlug = 123456,
                    IgdbId = 132456,
                    Added = DateTime.UtcNow,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });

                c.Insert.IntoTable("GameTranslations").Row(new
                {
                    GameId = 5,
                    Title = "Some Trans",
                    Language = 1
                });
            });

            var metadata = db.Query<GameMetadata207>("SELECT \"Id\", \"Title\", \"IgdbId\" FROM \"GameMetadata\"");

            metadata.Should().HaveCount(1);
            metadata.First().IgdbId.Should().Be(132456);
            metadata.First().Title.Should().Be("Title");

            var games = db.Query<Game207>("SELECT \"Id\", \"GameMetadataId\" FROM \"GameTranslations\"");

            games.Should().HaveCount(1);
            games.First().GameMetadataId.Should().Be(metadata.First().Id);
        }

        [Test]
        public void should_add_metadata_from_list_and_link_back()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("ImportListGames").Row(new
                {
                    Title = "Title",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Runtime = 90,
                    IgdbId = 123456,
                    ListId = 4,
                    Translations = new[] { new { } }.ToJson(),
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                });
            });

            var metadata = db.Query<GameMetadata207>("SELECT \"Id\", \"Title\", \"IgdbId\" FROM \"GameMetadata\"");

            metadata.Should().HaveCount(1);
            metadata.First().IgdbId.Should().Be(123456);
            metadata.First().Title.Should().Be("Title");

            var games = db.Query<Game207>("SELECT \"Id\", \"GameMetadataId\" FROM \"ImportListGames\"");

            games.Should().HaveCount(1);
            games.First().GameMetadataId.Should().Be(metadata.First().Id);
        }

        [Test]
        public void should_not_duplicate_metadata()
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
                    Runtime = 90,
                    OriginalLanguage = 1,
                    ProfileId = 1,
                    GameFileId = 0,
                    Path = string.Format("/Games/{0}", "Title"),
                    TitleSlug = 123456,
                    IgdbId = 123456,
                    Added = DateTime.UtcNow,
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                    LastInfoSync = DateTime.UtcNow,
                });

                c.Insert.IntoTable("ImportListGames").Row(new
                {
                    Title = "Title",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Runtime = 90,
                    IgdbId = 123456,
                    ListId = 4,
                    Translations = new[] { new { } }.ToJson(),
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                });
            });

            var metadata = db.Query<GameMetadata207>("SELECT \"Id\", \"Title\", \"IgdbId\" FROM \"GameMetadata\"");

            metadata.Should().HaveCount(1);
            metadata.First().IgdbId.Should().Be(123456);
            metadata.First().Title.Should().Be("Title");

            var games = db.Query<Game207>("SELECT \"Id\", \"GameMetadataId\" FROM \"Games\"");

            games.Should().HaveCount(1);
            games.First().GameMetadataId.Should().Be(metadata.First().Id);

            var listGames = db.Query<Game207>("SELECT \"Id\", \"GameMetadataId\" FROM \"ImportListGames\"");

            listGames.Should().HaveCount(1);
            listGames.First().GameMetadataId.Should().Be(metadata.First().Id);
        }

        [Test]
        public void should_not_duplicate_metadata_from_lists()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("ImportListGames").Row(new
                {
                    Title = "Title",
                    Overview = "Overview 1",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Runtime = 90,
                    IgdbId = 123456,
                    ListId = 4,
                    Translations = new[] { new { } }.ToJson(),
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                });

                c.Insert.IntoTable("ImportListGames").Row(new
                {
                    Title = "Title",
                    Overview = "Overview 2",
                    Status = 3,
                    Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                    Runtime = 90,
                    IgdbId = 123456,
                    ListId = 5,
                    Translations = new[] { new { } }.ToJson(),
                    Collection = new { Name = "Some Collection", IgdbId = 11 }.ToJson(),
                });
            });

            var metadata = db.Query<GameMetadata207>("SELECT \"Id\", \"Title\", \"IgdbId\" FROM \"GameMetadata\"");

            metadata.Should().HaveCount(1);
            metadata.First().IgdbId.Should().Be(123456);
            metadata.First().Title.Should().Be("Title");

            var listGames = db.Query<Game207>("SELECT \"Id\", \"GameMetadataId\" FROM \"ImportListGames\"");

            listGames.Should().HaveCount(2);
            listGames.First().GameMetadataId.Should().Be(metadata.First().Id);
        }
    }

    public class GameMetadata207
    {
        public int Id { get; set; }
        public int IgdbId { get; set; }
        public string Title { get; set; }
        public bool Monitored { get; set; }
    }

    public class Game207
    {
        public int Id { get; set; }
        public int GameMetadataId { get; set; }
    }
}
