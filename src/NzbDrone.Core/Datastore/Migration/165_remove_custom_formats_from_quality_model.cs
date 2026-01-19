using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(165)]
    public class remove_custom_formats_from_quality_model : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Blacklist").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);
            Alter.Table("GameFiles").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);

            // Switch Quality and Language to int in pending releases, remove custom formats
            Execute.WithConnection(FixPendingReleases);

            // Remove Custom Formats from QualityModel
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<QualityModel165>());
            Execute.WithConnection((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "Blacklist"));
            Execute.WithConnection((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "History"));
            Execute.WithConnection((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "GameFiles"));

            // Fish out indexer flags from history
            Execute.WithConnection(AddIndexerFlagsToBlacklist);
            Execute.WithConnection(AddIndexerFlagsToGameFiles);
        }

        private void FixPendingReleases(IDbConnection conn, IDbTransaction tran)
        {
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedGameInfo164>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedGameInfo165>());
            var rows = conn.Query<ParsedGameInfoData164>("SELECT \"Id\", \"ParsedGameInfo\" from \"PendingReleases\"");

            var newRows = new List<ParsedGameInfoData165>();

            foreach (var row in rows)
            {
                var old = row.ParsedGameInfo;

                var newQuality = new QualityModel165
                {
                    Quality = old.Quality.Quality.Id,
                    Revision = old.Quality.Revision,
                    HardcodedSubs = old.Quality.HardcodedSubs
                };

                var languages = old.Languages?.Select(x => (Language)x).Select(x => x.Id).ToList();

                var correct = new ParsedGameInfo165
                {
                    GameTitle = old.GameTitle,
                    SimpleReleaseTitle = old.SimpleReleaseTitle,
                    Quality = newQuality,
                    Languages = languages,
                    ReleaseGroup = old.ReleaseGroup,
                    ReleaseHash = old.ReleaseHash,
                    Edition = old.Edition,
                    Year = old.Year,
                    ImdbId = old.ImdbId
                };

                newRows.Add(new ParsedGameInfoData165
                {
                    Id = row.Id,
                    ParsedGameInfo = correct
                });
            }

            var sql = $"UPDATE \"PendingReleases\" SET \"ParsedGameInfo\" = @ParsedGameInfo WHERE \"Id\" = @Id";

            conn.Execute(sql, newRows, transaction: tran);
        }

        private void RemoveCustomFormatFromQuality(IDbConnection conn, IDbTransaction tran, string table)
        {
            var rows = conn.Query<QualityRow>($"SELECT \"Id\", \"Quality\" from \"{table}\"");

            var sql = $"UPDATE \"{table}\" SET \"Quality\" = @Quality WHERE \"Id\" = @Id";

            conn.Execute(sql, rows, transaction: tran);
        }

        private void AddIndexerFlagsToBlacklist(IDbConnection conn, IDbTransaction tran)
        {
            var blacklists = conn.Query<BlacklistData>("SELECT \"Blacklist\".\"Id\", \"Blacklist\".\"TorrentInfoHash\", \"History\".\"Data\" " +
                                                       "FROM \"Blacklist\" " +
                                                       "JOIN \"History\" ON \"Blacklist\".\"GameId\" = \"History\".\"GameId\" " +
                                                       "WHERE \"History\".\"EventType\" = 1");

            var toUpdate = new List<IndexerFlagsItem>();

            foreach (var item in blacklists)
            {
                var dict = Json.Deserialize<Dictionary<string, string>>(item.Data);

                if (dict.GetValueOrDefault("torrentInfoHash") == item.TorrentInfoHash &&
                    Enum.TryParse(dict.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                {
                    if (flags != 0)
                    {
                        toUpdate.Add(new IndexerFlagsItem
                        {
                            Id = item.Id,
                            IndexerFlags = (int)flags
                        });
                    }
                }
            }

            var updateSql = "UPDATE \"Blacklist\" SET \"IndexerFlags\" = @IndexerFlags WHERE \"Id\" = @Id";
            conn.Execute(updateSql, toUpdate, transaction: tran);
        }

        private void AddIndexerFlagsToGameFiles(IDbConnection conn, IDbTransaction tran)
        {
            var gameFiles = conn.Query<GameFileData>("SELECT \"GameFiles\".\"Id\", \"GameFiles\".\"SceneName\", \"History\".\"SourceTitle\", \"History\".\"Data\" " +
                                                       "FROM \"GameFiles\" " +
                                                       "JOIN \"History\" ON \"GameFiles\".\"GameId\" = \"History\".\"GameId\" " +
                                                       "WHERE \"History\".\"EventType\" = 1");

            var toUpdate = new List<IndexerFlagsItem>();

            foreach (var item in gameFiles)
            {
                var dict = Json.Deserialize<Dictionary<string, string>>(item.Data);

                if (item.SourceTitle == item.SceneName &&
                    Enum.TryParse(dict.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                {
                    if (flags != 0)
                    {
                        toUpdate.Add(new IndexerFlagsItem
                        {
                            Id = item.Id,
                            IndexerFlags = (int)flags
                        });
                    }
                }
            }

            var updateSql = "UPDATE \"GameFiles\" SET \"IndexerFlags\" = @IndexerFlags WHERE \"Id\" = @Id";
            conn.Execute(updateSql, toUpdate, transaction: tran);
        }

        private class ParsedGameInfoData164 : ModelBase
        {
            public ParsedGameInfo164 ParsedGameInfo { get; set; }
        }

        private class ParsedGameInfo164
        {
            public string GameTitle { get; set; }
            public string SimpleReleaseTitle { get; set; }
            public QualityModel164 Quality { get; set; }
            public List<string> Languages { get; set; }
            public string ReleaseGroup { get; set; }
            public string ReleaseHash { get; set; }
            public string Edition { get; set; }
            public int Year { get; set; }
            public string ImdbId { get; set; }
        }

        private class QualityModel164
        {
            public Quality164 Quality { get; set; }
            public Revision165 Revision { get; set; }
            public string HardcodedSubs { get; set; }
        }

        private class Quality164
        {
            public int Id { get; set; }
        }

        private class ParsedGameInfoData165 : ModelBase
        {
            public ParsedGameInfo165 ParsedGameInfo { get; set; }
        }

        private class ParsedGameInfo165
        {
            public string GameTitle { get; set; }
            public string SimpleReleaseTitle { get; set; }
            public QualityModel165 Quality { get; set; }
            public List<int> Languages { get; set; }
            public string ReleaseGroup { get; set; }
            public string ReleaseHash { get; set; }
            public string Edition { get; set; }
            public int Year { get; set; }
            public string ImdbId { get; set; }
        }

        private class BlacklistData : ModelBase
        {
            public string TorrentInfoHash { get; set; }
            public string Data { get; set; }
        }

        private class GameFileData : ModelBase
        {
            public string SceneName { get; set; }
            public string SourceTitle { get; set; }
            public string Data { get; set; }
        }

        private class IndexerFlagsItem : ModelBase
        {
            public int IndexerFlags { get; set; }
        }

        private class QualityRow : ModelBase
        {
            public QualityModel165 Quality { get; set; }
        }

        private class QualityModel165
        {
            public int Quality { get; set; }
            public Revision165 Revision { get; set; }
            public string HardcodedSubs { get; set; }
        }

        private class Revision165
        {
            public int Version { get; set; }
            public int Real { get; set; }
            public bool IsRepack { get; set; }
        }
    }
}
