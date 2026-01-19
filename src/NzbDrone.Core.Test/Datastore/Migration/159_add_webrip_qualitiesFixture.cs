using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class add_webrip_qualitesFixture : MigrationTest<add_webrip_qualites>
    {
        private string GenerateQualityJson(int quality, bool allowed)
        {
            return $"{{ \"quality\": {quality}, \"allowed\": {allowed.ToString().ToLowerInvariant()} }}";
        }

        private string GenerateQualityGroupJson(int quality, bool allowed, string groupname, int group)
        {
            return $"{{\"id\": {group}, \"name\": \"{groupname}\", \"items\": [ {{ \"quality\": {quality}, \"allowed\": {allowed.ToString().ToLowerInvariant()} }} ] }}";
        }

        [Test]
        public void should_add_webrip_qualities_and_group_with_webdl()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Profiles").Row(new
                {
                    Id = 0,
                    Name = "SDTV",
                    Cutoff = 1,
                    Items = $"[{GenerateQualityJson(1, true)}, {GenerateQualityJson((int)Quality.Scene, false)}, {GenerateQualityJson((int)Quality.Epic, false)}, {GenerateQualityJson((int)Quality.Steam, false)}, {GenerateQualityJson((int)Quality.RepackAllDLC, false)}]"
                });
            });

            var profiles = db.Query<Profile159>("SELECT \"Items\" FROM \"Profiles\" LIMIT 1");

            var items = profiles.First().Items;
            items.Should().HaveCount(5);
            items.Select(v => v.Quality).Should().Equal(1, null, null, null, null);
            items.Select(v => v.Items.Count).Should().Equal(0, 2, 2, 2, 2);
            items.Select(v => v.Allowed).Should().Equal(true, false, false, false, false);
        }

        [Test]
        public void should_add_webrip_and_webdl_if_webdl_is_missing()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Profiles").Row(new
                {
                    Id = 0,
                    Name = "SDTV",
                    Cutoff = 1,
                    Items = $"[{GenerateQualityJson(1, true)}, {GenerateQualityJson((int)Quality.Scene, false)}, {GenerateQualityJson((int)Quality.Epic, false)}, {GenerateQualityJson((int)Quality.Steam, false)}]"
                });
            });

            var profiles = db.Query<Profile159>("SELECT \"Items\" FROM \"Profiles\" LIMIT 1");

            var items = profiles.First().Items;
            items.Should().HaveCount(5);
            items.Select(v => v.Quality).Should().Equal(1, null, null, null, null);
            items.Select(v => v.Items.Count).Should().Equal(0, 2, 2, 2, 2);
            items.Select(v => v.Allowed).Should().Equal(true, false, false, false, false);
        }

        [Test]
        public void should_add_webrip_beside_webdl_is_grouped()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Profiles").Row(new
                {
                    Id = 0,
                    Name = "SDTV",
                    Cutoff = 1,
                    Items = $"[{GenerateQualityJson(1, true)}, {GenerateQualityGroupJson(5, true, "smegrup", 1001)}]"
                });
            });

            var profiles = db.Query<Profile159>("SELECT \"Items\" FROM \"Profiles\" LIMIT 1");

            var items = profiles.First().Items;
            items.Count(c => c.Id == 1001).Should().Be(1);
            items.Should().HaveCount(5);
            items.Select(v => v.Quality).Should().Equal(1, null, null, null, null);
            items.Select(v => v.Items.Count).Should().Equal(0, 2, 2, 2, 2);
            items.Select(v => v.Allowed).Should().Equal(true, false, false, false, false);
        }

        [Test]
        public void should_group_webrip_and_webdl_with_the_same_resolution()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Profiles").Row(new
                {
                    Id = 0,
                    Name = "SDTV",
                    Cutoff = 1,
                    Items = $"[{GenerateQualityJson(1, true)}, {GenerateQualityJson((int)Quality.Scene, false)}, {GenerateQualityJson((int)Quality.Epic, false)}, {GenerateQualityJson((int)Quality.Steam, false)}, {GenerateQualityJson((int)Quality.RepackAllDLC, false)}]"
                });
            });

            var profiles = db.Query<Profile159>("SELECT \"Items\" FROM \"Profiles\" LIMIT 1");
            var items = profiles.First().Items;

            items[1].Items.First().Quality.Should().Be((int)Quality.Scene);
            items[1].Items.Last().Quality.Should().Be((int)Quality.Scene);

            items[2].Items.First().Quality.Should().Be((int)Quality.Epic);
            items[2].Items.Last().Quality.Should().Be((int)Quality.Epic);

            items[3].Items.First().Quality.Should().Be((int)Quality.Steam);
            items[3].Items.Last().Quality.Should().Be((int)Quality.Steam);

            items[4].Items.First().Quality.Should().Be((int)Quality.RepackAllDLC);
            items[4].Items.Last().Quality.Should().Be((int)Quality.RepackAllDLC);
        }
    }
}
