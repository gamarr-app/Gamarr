using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(177)]
    public class language_improvements : NzbDroneMigrationBase
    {
        private readonly JsonSerializerOptions _serializerSettings;

        public language_improvements()
        {
            _serializerSettings = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                PropertyNameCaseInsensitive = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        protected override void MainDbUpgrade()
        {
            // Use original language to set default language fallback for releases
            // Set all to English (1) on migration to ensure default behavior persists until refresh
            Alter.Table("Games").AddColumn("OriginalLanguage").AsInt32().WithDefaultValue((int)Language.English);
            Alter.Table("Games").AddColumn("OriginalTitle").AsString().Nullable();

            Alter.Table("Games").AddColumn("DigitalRelease").AsDateTime().Nullable();

            // Column not used
            Delete.Column("PhysicalReleaseNote").FromTable("Games");
            Delete.Column("SecondaryYearSourceId").FromTable("Games");

            Alter.Table("NamingConfig").AddColumn("RenameGames").AsBoolean().WithDefaultValue(false);
            Execute.Sql("UPDATE \"NamingConfig\" SET \"RenameGames\"=\"RenameEpisodes\"");
            Delete.Column("RenameEpisodes").FromTable("NamingConfig");

            // Manual SQL, Fluent Migrator doesn't support multi-column unique constraint on table creation, SQLite doesn't support adding it after creation
            IfDatabase("sqlite").Execute.Sql("CREATE TABLE \"GameTranslations\"(" +
                "\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "\"GameId\" INTEGER NOT NULL, " +
                "\"Title\" TEXT, " +
                "\"CleanTitle\" TEXT, " +
                "\"Overview\" TEXT, " +
                "\"Language\" INTEGER NOT NULL, " +
                "Unique(\"GameId\", \"Language\"));");

            IfDatabase("postgres").Execute.Sql("CREATE TABLE \"GameTranslations\"(" +
                "\"Id\" SERIAL PRIMARY KEY , " +
                "\"GameId\" INTEGER NOT NULL, " +
                "\"Title\" TEXT, " +
                "\"CleanTitle\" TEXT, " +
                "\"Overview\" TEXT, " +
                "\"Language\" INTEGER NOT NULL, " +
                "Unique(\"GameId\", \"Language\"));");

            // Prevent failure if two games have same alt titles
            Execute.Sql("DROP INDEX IF EXISTS \"IX_AlternativeTitles_CleanTitle\"");

            Execute.WithConnection(FixLanguagesMoveFile);
            Execute.WithConnection(FixLanguagesHistory);

            // Force refresh all games in library
            Update.Table("ScheduledTasks")
                .Set(new { LastExecution = "2014-01-01 00:00:00" })
                .Where(new { TypeName = "NzbDrone.Core.Games.Commands.RefreshGameCommand" });

            Update.Table("Games")
                .Set(new { LastInfoSync = "2014-01-01 00:00:00" })
                .AllRows();
        }

        private void FixLanguagesMoveFile(IDbConnection conn, IDbTransaction tran)
        {
            var rows = conn.Query<LanguageEntity177>($"SELECT \"Id\", \"Languages\" FROM \"GameFiles\"");

            var corrected = new List<LanguageEntity177>();

            foreach (var row in rows)
            {
                var languages = JsonSerializer.Deserialize<List<int>>(row.Languages, _serializerSettings);

                var newLanguages = languages.Distinct().ToList();

                corrected.Add(new LanguageEntity177
                {
                    Id = row.Id,
                    Languages = JsonSerializer.Serialize(newLanguages, _serializerSettings)
                });
            }

            var updateSql = "UPDATE \"GameFiles\" SET \"Languages\" = @Languages WHERE \"Id\" = @Id";
            conn.Execute(updateSql, corrected, transaction: tran);
        }

        private void FixLanguagesHistory(IDbConnection conn, IDbTransaction tran)
        {
            var rows = conn.Query<LanguageEntity177>($"SELECT \"Id\", \"Languages\" FROM \"History\"");

            var corrected = new List<LanguageEntity177>();

            foreach (var row in rows)
            {
                var languages = JsonSerializer.Deserialize<List<int>>(row.Languages, _serializerSettings);

                var newLanguages = languages.Distinct().ToList();

                corrected.Add(new LanguageEntity177
                {
                    Id = row.Id,
                    Languages = JsonSerializer.Serialize(newLanguages, _serializerSettings)
                });
            }

            var updateSql = "UPDATE \"History\" SET \"Languages\" = @Languages WHERE \"Id\" = @Id";
            conn.Execute(updateSql, corrected, transaction: tran);
        }

        private class LanguageEntity177 : ModelBase
        {
            public string Languages { get; set; }
        }
    }
}
