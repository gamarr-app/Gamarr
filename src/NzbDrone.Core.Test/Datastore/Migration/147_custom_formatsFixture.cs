using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class custom_formatsFixture : MigrationTest<add_custom_formats>
    {
        public static Dictionary<int, int> QualityToDefinition;

        public void AddDefaultProfile(add_custom_formats m, string name, Quality cutoff, params Quality[] allowed)
        {
            var items = Quality.DefaultQualityDefinitions
                .OrderBy(v => v.Weight)
                .Select(v => new { Quality = (int)v.Quality, Allowed = allowed.Contains(v.Quality) })
                .ToList();

            var profile = new { Name = name, Cutoff = (int)cutoff, Items = items.ToJson(), Language = (int)Language.English };

            m.Insert.IntoTable("Profiles").Row(profile);
        }

        public void WithDefaultProfiles(add_custom_formats m)
        {
            AddDefaultProfile(m,
                "Any",
                Quality.Scene,
                Quality.Preload,
                Quality.Preload,
                Quality.UpdateOnly,
                Quality.UpdateOnly,
                Quality.SceneCracked,
                Quality.MultiLang,
                Quality.Scene,
                Quality.Scene,
                Quality.SceneCracked,
                Quality.Uplay,
                Quality.Origin,
                Quality.ISO,
                Quality.Scene,
                Quality.Epic,
                Quality.Steam,
                Quality.RepackAllDLC,
                Quality.Scene,
                Quality.Scene,
                Quality.Repack,
                Quality.GOG,
                Quality.RepackAllDLC,
                Quality.GOG,
                Quality.RepackAllDLC,
                Quality.ISO);

            AddDefaultProfile(m,
                "SD",
                Quality.Scene,
                Quality.Preload,
                Quality.Preload,
                Quality.UpdateOnly,
                Quality.UpdateOnly,
                Quality.SceneCracked,
                Quality.MultiLang,
                Quality.Scene,
                Quality.Scene,
                Quality.Scene,
                Quality.Scene,
                Quality.Scene);

            AddDefaultProfile(m,
                "HD-720p",
                Quality.Repack,
                Quality.Uplay,
                Quality.Epic,
                Quality.Repack);

            AddDefaultProfile(m,
                "HD-1080p",
                Quality.GOG,
                Quality.Origin,
                Quality.Steam,
                Quality.GOG,
                Quality.GOG);

            AddDefaultProfile(m,
                "Ultra-HD",
                Quality.RepackAllDLC,
                Quality.ISO,
                Quality.RepackAllDLC,
                Quality.RepackAllDLC,
                Quality.RepackAllDLC);

            AddDefaultProfile(m,
                "HD - 720p/1080p",
                Quality.Repack,
                Quality.Uplay,
                Quality.Origin,
                Quality.Epic,
                Quality.Steam,
                Quality.Repack,
                Quality.GOG,
                Quality.GOG,
                Quality.RepackAllDLC);
        }

        [Test]
        public void should_correctly_update_items_of_default_profiles()
        {
            var db = WithMigrationTestDb(c =>
            {
                WithDefaultProfiles(c);
            });

            ShouldHaveAddedDefaultFormat(db);
        }

        private void ShouldHaveAddedDefaultFormat(IDirectDataMapper db)
        {
            var items = QueryItems(db);

            foreach (var item in items)
            {
                item.DeserializedItems.Count.Should().Be(1);
                item.DeserializedItems.First().Allowed.Should().Be(true);
                item.FormatCutoff.Should().Be(0);
            }
        }

        private List<Profile147> QueryItems(IDirectDataMapper db)
        {
            var test = db.Query("SELECT * FROM \"Profiles\"");

            var items = db.Query<Profile147>("SELECT \"FormatItems\", \"FormatCutoff\" FROM \"Profiles\"");

            return items.Select(i =>
            {
                i.DeserializedItems = JsonConvert.DeserializeObject<List<ProfileFormatItem147>>(i.FormatItems);
                return i;
            }).ToList();
        }

        [Test]
        public void should_correctly_migrate_custom_profile()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Quality.Epic, Quality.Epic, Quality.Steam);
            });

            ShouldHaveAddedDefaultFormat(db);
        }

        public class Profile147
        {
            public string FormatItems { get; set; }
            public List<ProfileFormatItem147> DeserializedItems;
            public int FormatCutoff { get; set; }
        }

        public class ProfileFormatItem147
        {
            public int Format;
            public bool Allowed;
        }
    }
}
