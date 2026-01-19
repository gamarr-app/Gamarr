using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class fix_igdb_duplicatesFixture : MigrationTest<fix_igdb_duplicates>
    {
        private void AddGame(fix_igdb_duplicates m, int id, string gameTitle, string titleSlug, int igdbId, int gameFileId, DateTime? lastInfo, DateTime added)
        {
            var game = new
            {
                Id = id,
                Monitored = true,
                Title = gameTitle,
                CleanTitle = gameTitle,
                Status = (int)GameStatusType.Announced,
                MinimumAvailability = (int)GameStatusType.Announced,
                Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                Recommendations = new[] { 1 }.ToJson(),
                HasPreDBEntry = false,
                Runtime = 90,
                OriginalLanguage = 1,
                ProfileId = 1,
                GameFileId = gameFileId,
                Path = string.Format("/Games/{0}", gameTitle),
                TitleSlug = titleSlug,
                IgdbId = igdbId,
                Added = added,
                LastInfoSync = lastInfo,
            };

            m.Insert.IntoTable("Games").Row(game);
        }

        [Test]
        public void should_clean_duplicate_games()
        {
            var igdbId = 123465;
            var dateAdded = DateTime.UtcNow;

            var db = WithMigrationTestDb(c =>
            {
                AddGame(c, 1, "game", "slug", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 2, "game", "slug1", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 3, "game", "slug2", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 4, "game", "slug3", igdbId, 0, dateAdded, dateAdded);
            });

            var items = db.Query<Game185>("SELECT \"Id\", \"IgdbId\", \"GameFileId\" FROM \"Games\"");

            items.Should().HaveCount(1);
        }

        [Test]
        public void should_not_clean_non_duplicate_games()
        {
            var igdbId = 123465;
            var dateAdded = DateTime.UtcNow;

            var db = WithMigrationTestDb(c =>
            {
                AddGame(c, 1, "game", "slug", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 2, "game", "slug1", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 3, "game", "slug2", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 4, "game", "slug3", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 5, "game2", "slug4", 123457, 0, dateAdded, dateAdded);
            });

            var items = db.Query<Game185>("SELECT \"Id\", \"IgdbId\", \"GameFileId\" FROM \"Games\"");

            items.Should().HaveCount(2);
            items.Where(i => i.IgdbId == igdbId).Should().HaveCount(1);
        }

        [Test]
        public void should_not_clean_any_if_no_duplicate_games()
        {
            var dateAdded = DateTime.UtcNow;

            var db = WithMigrationTestDb(c =>
            {
                AddGame(c, 1, "game1", "slug", 1, 0, dateAdded, dateAdded);
                AddGame(c, 2, "game2", "slug1", 2, 0, dateAdded, dateAdded);
                AddGame(c, 3, "game3", "slug2", 3, 0, dateAdded, dateAdded);
                AddGame(c, 4, "game4", "slug3", 4, 0, dateAdded, dateAdded);
                AddGame(c, 5, "game5", "slug4", 123457, 0, dateAdded, dateAdded);
            });

            var items = db.Query<Game185>("SELECT \"Id\", \"IgdbId\", \"GameFileId\" FROM \"Games\"");

            items.Should().HaveCount(5);
        }

        [Test]
        public void should_keep_game_with_file_when_duplicates()
        {
            var igdbId = 123465;
            var dateAdded = DateTime.UtcNow;

            var db = WithMigrationTestDb(c =>
            {
                AddGame(c, 1, "game", "slug", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 2, "game", "slug1", igdbId, 1, dateAdded, dateAdded);
                AddGame(c, 3, "game", "slug2", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 4, "game", "slug3", igdbId, 0, dateAdded, dateAdded);
            });

            var items = db.Query<Game185>("SELECT \"Id\", \"IgdbId\", \"GameFileId\" FROM \"Games\"");

            items.Should().HaveCount(1);
            items.First().Id.Should().Be(2);
        }

        [Test]
        public void should_keep_earliest_added_a_game_with_file_when_duplicates_and_multiple_have_file()
        {
            var igdbId = 123465;
            var dateAdded = DateTime.UtcNow;

            var db = WithMigrationTestDb(c =>
            {
                AddGame(c, 1, "game", "slug", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 2, "game", "slug1", igdbId, 1, dateAdded, dateAdded.AddSeconds(200));
                AddGame(c, 3, "game", "slug2", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 4, "game", "slug3", igdbId, 2, dateAdded, dateAdded);
            });

            var items = db.Query<Game185>("SELECT \"Id\", \"IgdbId\", \"GameFileId\" FROM \"Games\"");

            items.Should().HaveCount(1);
            items.First().GameFileId.Should().BeGreaterThan(0);
            items.First().Id.Should().Be(4);
        }

        [Test]
        public void should_keep_a_game_with_info_when_duplicates_and_no_file()
        {
            var igdbId = 123465;
            var dateAdded = DateTime.UtcNow;

            var db = WithMigrationTestDb(c =>
            {
                AddGame(c, 1, "game", "slug", igdbId, 0, null, dateAdded);
                AddGame(c, 2, "game", "slug1", igdbId, 0, null, dateAdded);
                AddGame(c, 3, "game", "slug2", igdbId, 0, dateAdded, dateAdded);
                AddGame(c, 4, "game", "slug3", igdbId, 0, null, dateAdded);
            });

            var items = db.Query<Game185>("SELECT \"Id\", \"LastInfoSync\", \"IgdbId\", \"GameFileId\" FROM \"Games\"");

            items.Should().HaveCount(1);
            items.First().LastInfoSync.Should().NotBeNull();
        }

        public class Game185
        {
            public int Id { get; set; }
            public int IgdbId { get; set; }
            public int GameFileId { get; set; }
            public DateTime? LastInfoSync { get; set; }
        }
    }
}
