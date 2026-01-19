using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Datastore.Migration
{
    // this is here to resolve ambiguity in GetValueOrDefault extension method in net core 3
#pragma warning disable SA1200
    using NzbDrone.Common.Extensions;
#pragma warning restore SA1200

    [Migration(154)]
    public class add_language_to_files_history_blacklist : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("GameFiles")
                 .AddColumn("Languages").AsString().NotNullable().WithDefaultValue("[]");

            Alter.Table("History")
                 .AddColumn("Languages").AsString().NotNullable().WithDefaultValue("[]");

            Alter.Table("Blacklist")
                 .AddColumn("Languages").AsString().NotNullable().WithDefaultValue("[]");

            Execute.WithConnection(UpdateLanguage);
        }

        private void UpdateLanguage(IDbConnection conn, IDbTransaction tran)
        {
            var languageConverter = new EmbeddedDocumentConverter<List<Language>>(new LanguageIntConverter());

            var profileLanguages = new Dictionary<int, int>();
            using (var getProfileCmd = conn.CreateCommand())
            {
                getProfileCmd.Transaction = tran;
                getProfileCmd.CommandText = "SELECT \"Id\", \"Language\" FROM \"Profiles\"";

                var profilesReader = getProfileCmd.ExecuteReader();
                while (profilesReader.Read())
                {
                    var profileId = profilesReader.GetInt32(0);
                    var gameLanguage = Language.English.Id;
                    try
                    {
                        gameLanguage = profilesReader.GetInt32(1) != -1 ? profilesReader.GetInt32(1) : 1;
                    }
                    catch (InvalidCastException e)
                    {
                        _logger.Debug("Language field not found in Profiles, using English as default." + e.Message);
                    }

                    profileLanguages[profileId] = gameLanguage;
                }

                profilesReader.Close();
            }

            var gameLanguages = new Dictionary<int, int>();

            using (var getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = @"SELECT ""Id"", ""ProfileId"" FROM ""Games""";
                using (var gamesReader = getSeriesCmd.ExecuteReader())
                {
                    while (gamesReader.Read())
                    {
                        var gameId = gamesReader.GetInt32(0);
                        var gameProfileId = gamesReader.GetInt32(1);

                        gameLanguages[gameId] = profileLanguages.GetValueOrDefault(gameProfileId, Language.English.Id);
                    }

                    gamesReader.Close();
                }
            }

            var gameFileLanguages = new Dictionary<int, List<Language>>();
            var releaseLanguages = new Dictionary<string, List<Language>>();

            using (var getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = @"SELECT ""Id"", ""GameId"", ""SceneName"", ""MediaInfo"" FROM ""GameFiles""";
                using (var gameFilesReader = getSeriesCmd.ExecuteReader())
                {
                    while (gameFilesReader.Read())
                    {
                        var gameFileId = gameFilesReader.GetInt32(0);
                        var gameId = gameFilesReader.GetInt32(1);
                        var gameFileSceneName = gameFilesReader.IsDBNull(2) ? null : gameFilesReader.GetString(2);
                        var gameFileMediaInfo = gameFilesReader.IsDBNull(3) ? null : Json.Deserialize<MediaInfo154>(gameFilesReader.GetString(3));
                        var languages = new List<Language>();

                        if (gameFileMediaInfo != null && gameFileMediaInfo.AudioLanguages.IsNotNullOrWhiteSpace())
                        {
                            var mediaInfolanguages = gameFileMediaInfo.AudioLanguages.Split('/').Select(l => l.Trim()).Distinct().ToList();

                            foreach (var audioLanguage in mediaInfolanguages)
                            {
                                var language = IsoLanguages.FindByName(audioLanguage)?.Language;
                                languages.AddIfNotNull(language);
                            }
                        }

                        if (!languages.Any(l => l.Id != 0) && gameFileSceneName.IsNotNullOrWhiteSpace())
                        {
                            languages = LanguageParser.ParseLanguages(gameFileSceneName);
                        }

                        if (!languages.Any(l => l.Id != 0))
                        {
                            languages = new List<Language> { Language.FindById(gameLanguages[gameId]) };
                        }

                        if (gameFileSceneName.IsNotNullOrWhiteSpace())
                        {
                            // Store languages for this scenerelease so we can use in history later
                            releaseLanguages[gameFileSceneName] = languages;
                        }

                        gameFileLanguages[gameFileId] = languages;
                    }

                    gameFilesReader.Close();
                }
            }

            var historyLanguages = new Dictionary<int, List<Language>>();

            using (var getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = @"SELECT ""Id"", ""SourceTitle"", ""GameId"" FROM ""History""";
                using (var historyReader = getSeriesCmd.ExecuteReader())
                {
                    while (historyReader.Read())
                    {
                        var historyId = historyReader.GetInt32(0);
                        var historySourceTitle = historyReader.IsDBNull(1) ? null : historyReader.GetString(1);
                        var gameId = historyReader.GetInt32(2);
                        var languages = new List<Language>();

                        if (historySourceTitle.IsNotNullOrWhiteSpace() && releaseLanguages.ContainsKey(historySourceTitle))
                        {
                            languages = releaseLanguages[historySourceTitle];
                        }

                        if (!languages.Any(l => l.Id != 0) && historySourceTitle.IsNotNullOrWhiteSpace())
                        {
                            languages = LanguageParser.ParseLanguages(historySourceTitle);
                        }

                        if (!languages.Any(l => l.Id != 0))
                        {
                            languages = new List<Language> { Language.FindById(gameLanguages[gameId]) };
                        }

                        historyLanguages[historyId] = languages;
                    }

                    historyReader.Close();
                }
            }

            var blacklistLanguages = new Dictionary<int, List<Language>>();

            using (var getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = @"SELECT ""Id"", ""SourceTitle"", ""GameId"" FROM ""Blacklist""";
                using (var blacklistReader = getSeriesCmd.ExecuteReader())
                {
                    while (blacklistReader.Read())
                    {
                        var blacklistId = blacklistReader.GetInt32(0);
                        var blacklistSourceTitle = blacklistReader.IsDBNull(1) ? null : blacklistReader.GetString(1);
                        var gameId = blacklistReader.GetInt32(2);
                        var languages = new List<Language>();

                        if (blacklistSourceTitle.IsNotNullOrWhiteSpace())
                        {
                            languages = LanguageParser.ParseLanguages(blacklistSourceTitle);
                        }

                        if (!languages.Any(l => l.Id != 0))
                        {
                            languages = new List<Language> { Language.FindById(gameLanguages[gameId]) };
                        }

                        blacklistLanguages[blacklistId] = languages;
                    }

                    blacklistReader.Close();
                }
            }

            foreach (var group in gameFileLanguages.GroupBy(v => v.Value, v => v.Key))
            {
                var languages = group.Key;

                var gameFileIds = group.Select(v => v.ToString()).Join(",");

                using (var updateGameFilesCmd = conn.CreateCommand())
                {
                    updateGameFilesCmd.Transaction = tran;
                    if (conn.GetType().FullName == "Npgsql.NpgsqlConnection")
                    {
                        updateGameFilesCmd.CommandText = $"UPDATE \"GameFiles\" SET \"Languages\" = $1 WHERE \"Id\" IN ({gameFileIds})";
                    }
                    else
                    {
                        updateGameFilesCmd.CommandText = $"UPDATE \"GameFiles\" SET \"Languages\" = ? WHERE \"Id\" IN ({gameFileIds})";
                    }

                    var param = updateGameFilesCmd.CreateParameter();
                    languageConverter.SetValue(param, languages);
                    updateGameFilesCmd.Parameters.Add(param);

                    updateGameFilesCmd.ExecuteNonQuery();
                }
            }

            foreach (var group in historyLanguages.GroupBy(v => v.Value, v => v.Key))
            {
                var languages = group.Key;

                var historyIds = group.Select(v => v.ToString()).Join(",");

                using (var updateHistoryCmd = conn.CreateCommand())
                {
                    updateHistoryCmd.Transaction = tran;
                    if (conn.GetType().FullName == "Npgsql.NpgsqlConnection")
                    {
                        updateHistoryCmd.CommandText = $"UPDATE \"History\" SET \"Languages\" = $1 WHERE \"Id\" IN ({historyIds})";
                    }
                    else
                    {
                        updateHistoryCmd.CommandText = $"UPDATE \"History\" SET \"Languages\" = ? WHERE \"Id\" IN ({historyIds})";
                    }

                    var param = updateHistoryCmd.CreateParameter();
                    languageConverter.SetValue(param, languages);
                    updateHistoryCmd.Parameters.Add(param);

                    updateHistoryCmd.ExecuteNonQuery();
                }
            }

            foreach (var group in blacklistLanguages.GroupBy(v => v.Value, v => v.Key))
            {
                var languages = group.Key;

                var blacklistIds = group.Select(v => v.ToString()).Join(",");

                using (var updateBlacklistCmd = conn.CreateCommand())
                {
                    updateBlacklistCmd.Transaction = tran;
                    if (conn.GetType().FullName == "Npgsql.NpgsqlConnection")
                    {
                        updateBlacklistCmd.CommandText = $"UPDATE \"Blacklist\" SET \"Languages\" = $1 WHERE \"Id\" IN ({blacklistIds})";
                    }
                    else
                    {
                        updateBlacklistCmd.CommandText = $"UPDATE \"Blacklist\" SET \"Languages\" = ? WHERE \"Id\" IN ({blacklistIds})";
                    }

                    var param = updateBlacklistCmd.CreateParameter();
                    languageConverter.SetValue(param, languages);
                    updateBlacklistCmd.Parameters.Add(param);

                    updateBlacklistCmd.ExecuteNonQuery();
                }
            }
        }
    }

    public class MediaInfo154
    {
        public string AudioLanguages { get; set; }
    }
}
