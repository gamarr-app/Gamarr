using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class add_language_to_files_history_blacklistFixture : MigrationTest<add_language_to_files_history_blacklist>
    {
        private void AddDefaultProfile(add_language_to_files_history_blacklist m, string name, Language language)
        {
            var allowed = new Quality[] { Quality.WEBDL720p };

            var items = Quality.DefaultQualityDefinitions
                .OrderBy(v => v.Weight)
                .Select(v => new { Quality = (int)v.Quality, Allowed = allowed.Contains(v.Quality) })
                .ToList();

            var profile = new { Id = 1, Name = name, Cutoff = (int)Quality.WEBDL720p, Items = items.ToJson(), Language = (int)language };

            var game = new
            {
                Id = 1,
                Monitored = true,
                Title = "My Game",
                CleanTitle = "mytitle",
                Status = (int)GameStatusType.Announced,
                MinimumAvailability = (int)GameStatusType.Announced,
                Images = new[] { new { CoverType = "Poster" } }.ToJson(),
                HasPreDBEntry = false,
                PathState = 1,
                Runtime = 90,
                ProfileId = 1,
                GameFileId = 1,
                Path = "/Some/Path",
                TitleSlug = "123456",
                IgdbId = 123456
            };

            m.Insert.IntoTable("Profiles").Row(profile);
            m.Insert.IntoTable("Games").Row(game);
        }

        private void AddGameFile(add_language_to_files_history_blacklist m, string sceneName, string mediaInfoLanugaes)
        {
            m.Insert.IntoTable("GameFiles").Row(new
            {
                GameId = 1,
                Quality = new
                {
                    Quality = 6
                }.ToJson(),
                Size = 997478103,
                DateAdded = DateTime.Now,
                SceneName = sceneName,
                MediaInfo = new
                {
                    AudioLanguages = mediaInfoLanugaes
                }.ToJson(),
                RelativePath = "Never Say Never Again.1983.Bluray-720p.mp4",
            });
        }

        private void AddHistory(add_language_to_files_history_blacklist m, string sourceTitle)
        {
            m.Insert.IntoTable("History").Row(new
            {
                GameId = 1,
                Quality = new
                {
                    Quality = 6
                }.ToJson(),
                EventType = 1,
                Date = DateTime.Now,
                SourceTitle = sourceTitle,
                Data = new
                {
                    Indexer = "My Indexer"
                }.ToJson()
            });
        }

        private void AddBlacklist(add_language_to_files_history_blacklist m, string sourceTitle)
        {
            m.Insert.IntoTable("Blacklist").Row(new
            {
                GameId = 1,
                Quality = new
                {
                    Quality = 6
                }.ToJson(),
                Date = DateTime.Now,
                SourceTitle = sourceTitle,
                Size = 997478103
            });
        }

        [Test]
        public void should_add_languages_from_media_info_if_available()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddGameFile(c, "My.Game.2018.German.BluRay-Gamarr", "Japanese");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Japanese);
            items.First().Languages.Should().NotContain((int)Language.English);
            items.First().Languages.Should().NotContain((int)Language.Dutch);
        }

        [Test]
        public void should_add_languages_from_media_info_with_multiple_language()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddGameFile(c, "My.Game.2018.German.BluRay-Gamarr", "Japanese / French");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(2);
            items.First().Languages.Should().Contain((int)Language.Japanese);
            items.First().Languages.Should().Contain((int)Language.French);
            items.First().Languages.Should().NotContain((int)Language.English);
            items.First().Languages.Should().NotContain((int)Language.Dutch);
        }

        [Test]
        public void should_fallback_to_scenename_if_no_mediainfo_languages()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddGameFile(c, "My.Game.2018.German.BluRay-Gamarr", "");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.German);
            items.First().Languages.Should().NotContain((int)Language.Dutch);
        }

        [Test]
        public void should_fallback_to_scenename_if_mediainfo_language_invalid()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddGameFile(c, "My.Game.2018.German.BluRay-Gamarr", "English (USA)");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.German);
            items.First().Languages.Should().NotContain((int)Language.Dutch);
            items.First().Languages.Should().NotContain((int)Language.English);
        }

        [Test]
        public void should_fallback_to_profile_if_no_mediainfo_no_scene_name()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddGameFile(c, "", "");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Dutch);
            items.First().Languages.Should().NotContain((int)Language.Unknown);
        }

        [Test]
        public void should_handle_if_null_mediainfo_and_null_scenename()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddGameFile(c, null, null);
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Dutch);
            items.First().Languages.Should().NotContain((int)Language.Unknown);
        }

        [Test]
        public void should_fallback_to_profile_if_unknown_language_from_scene_name()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddGameFile(c, "My.Game.2018.BluRay-Gamarr", "");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Dutch);
            items.First().Languages.Should().NotContain((int)Language.Unknown);
        }

        [Test]
        public void should_use_english_if_fallback_to_profile_and_profile_is_any()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Any);
                AddGameFile(c, "", "");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.English);
            items.First().Languages.Should().NotContain((int)Language.Any);
            items.First().Languages.Should().NotContain((int)Language.Unknown);
        }

        [Test]
        public void should_assign_history_languages_from_sourceTitle()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddHistory(c, "My.Game.2018.Italian.BluRay-Gamarr");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"History\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Italian);
            items.First().Languages.Should().NotContain((int)Language.Dutch);
        }

        [Test]
        public void should_assign_history_languages_from_profile_if_no_sourceTitle()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddHistory(c, "");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"History\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Dutch);
            items.First().Languages.Should().NotContain((int)Language.Unknown);
        }

        [Test]
        public void should_assign_history_languages_from_profile_if_unknown_sourceTitle()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddHistory(c, "Man on Fire");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"History\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Dutch);
            items.First().Languages.Should().NotContain((int)Language.Unknown);
        }

        [Test]
        public void should_assign_history_languages_from_gamefile_release_mapping_with_mediainfo()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddGameFile(c, "My.Game.2018.Italian.BluRay-Gamarr", "Italian / French / German");
                AddHistory(c, "My.Game.2018.Italian.BluRay-Gamarr");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"History\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(3);
            items.First().Languages.Should().Contain((int)Language.Italian);
            items.First().Languages.Should().Contain((int)Language.French);
            items.First().Languages.Should().Contain((int)Language.German);
            items.First().Languages.Should().NotContain((int)Language.Dutch);
        }

        [Test]
        public void should_assign_blacklist_languages_from_sourceTitle()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddBlacklist(c, "My.Game.2018.Italian.BluRay-Gamarr");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"Blacklist\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Italian);
            items.First().Languages.Should().NotContain((int)Language.Dutch);
        }

        [Test]
        public void should_assign_blacklist_languages_from_profile_if_no_sourceTitle()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddBlacklist(c, "");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"Blacklist\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Dutch);
            items.First().Languages.Should().NotContain((int)Language.Unknown);
        }

        [Test]
        public void should_assign_blacklist_languages_from_profile_if_unknown_sourceTitle()
        {
            var db = WithMigrationTestDb(c =>
            {
                AddDefaultProfile(c, "My Custom Profile", Language.Dutch);
                AddBlacklist(c, "Man on Fire");
            });

            var items = db.Query<ModelWithLanguages154>("SELECT \"Id\", \"Languages\" FROM \"Blacklist\"");

            items.Should().HaveCount(1);
            items.First().Languages.Count.Should().Be(1);
            items.First().Languages.Should().Contain((int)Language.Dutch);
            items.First().Languages.Should().NotContain((int)Language.Unknown);
        }
    }

    public class ModelWithLanguages154
    {
        public int Id { get; set; }
        public List<int> Languages { get; set; }
    }
}
