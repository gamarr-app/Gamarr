using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Dapper;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    // Self-heal for libraries that already hold the same title twice on the
    // SAME platform (an add path that skipped the duplicate check, or two
    // overlapping adds). Duplicates make every title lookup ambiguous, so
    // releases for the game were rejected and downloads never imported.
    //
    // Entries for the same title on different platforms are the intended
    // per-platform setup (#150) and are never touched; nor is an Unknown +
    // platformed pair (the legacy upgrade path). Within a same-platform group
    // the entry that has files is kept; history, download history, blocklist,
    // pending releases and component slots move to it, monitoring and tags
    // are merged, and the other row is removed. If more than one entry has
    // files, the group is left alone and logged, since files can't be moved
    // between folders safely here. The removed entry's folder is deleted only
    // when it is empty. Its metadata copy, extra-file rows and other leftovers
    // go with the orphan housekeepers.
    public class CleanupDuplicateGames : IHousekeepingTask
    {
        private const string DeleteClashingComponentsSql = @"DELETE FROM ""GameComponents""
                                                                                   WHERE ""GameId"" = @LoserId
                                                                                   AND EXISTS (SELECT 1 FROM ""GameComponents"" AS ""Kept""
                                                                                               WHERE ""Kept"".""GameId"" = @KeeperId
                                                                                               AND ""Kept"".""ComponentType"" = ""GameComponents"".""ComponentType""
                                                                                               AND ""Kept"".""Key"" = ""GameComponents"".""Key"")";

        private readonly IMainDatabase _database;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public CleanupDuplicateGames(IMainDatabase database, IDiskProvider diskProvider, Logger logger)
        {
            _database = database;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public void Clean()
        {
            // IGDB first, then Steam: each pass re-reads, so a row merged away
            // by the first pass isn't grouped again by the second.
            CleanBy(r => r.IgdbId);
            CleanBy(r => r.SteamAppId);
        }

        private void CleanBy(Func<DuplicateGameRow, int> idSelector)
        {
            var groups = ReadRows()
                .Where(r => idSelector(r) > 0)
                .GroupBy(r => (Id: idSelector(r), r.Platform))
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in groups)
            {
                MergeGroup(group.OrderBy(r => r.Id).ToList());
            }
        }

        private void MergeGroup(List<DuplicateGameRow> rows)
        {
            var withFiles = rows.Where(r => r.FileCount > 0 || r.GameFileId > 0).ToList();

            if (withFiles.Count > 1)
            {
                _logger.Warn("Library holds '{0}' more than once for platform {1} and several entries have files ({2}); leaving them for manual cleanup",
                    rows[0].Title,
                    (PlatformFamily)rows[0].Platform,
                    string.Join(", ", withFiles.Select(r => r.Path)));

                return;
            }

            var keeper = withFiles.FirstOrDefault() ?? rows[0];
            var losers = rows.Where(r => r.Id != keeper.Id).ToList();

            var tags = ParseTags(keeper.Tags);
            var monitored = keeper.Monitored;

            using (var conn = _database.OpenConnection())
            using (var transaction = conn.BeginTransaction())
            {
                foreach (var loser in losers)
                {
                    var args = new { KeeperId = keeper.Id, LoserId = loser.Id };

                    conn.Execute(@"UPDATE ""History"" SET ""GameId"" = @KeeperId WHERE ""GameId"" = @LoserId", args, transaction);
                    conn.Execute(@"UPDATE ""DownloadHistory"" SET ""GameId"" = @KeeperId WHERE ""GameId"" = @LoserId", args, transaction);
                    conn.Execute(@"UPDATE ""Blocklist"" SET ""GameId"" = @KeeperId WHERE ""GameId"" = @LoserId", args, transaction);
                    conn.Execute(@"UPDATE ""PendingReleases"" SET ""GameId"" = @KeeperId WHERE ""GameId"" = @LoserId", args, transaction);

                    // A slot the keeper already has would be a duplicate slot.
                    conn.Execute(DeleteClashingComponentsSql, args, transaction);
                    conn.Execute(@"UPDATE ""GameComponents"" SET ""GameId"" = @KeeperId WHERE ""GameId"" = @LoserId", args, transaction);

                    conn.Execute(@"DELETE FROM ""Games"" WHERE ""Id"" = @LoserId", args, transaction);

                    tags.UnionWith(ParseTags(loser.Tags));
                    monitored |= loser.Monitored;
                }

                conn.Execute(@"UPDATE ""Games"" SET ""Monitored"" = @Monitored, ""Tags"" = @Tags WHERE ""Id"" = @Id",
                    new { Monitored = monitored, Tags = JsonSerializer.Serialize(tags.OrderBy(t => t)), Id = keeper.Id },
                    transaction);

                transaction.Commit();
            }

            foreach (var loser in losers)
            {
                _logger.Info("Merged duplicate library entry '{0}' ({1}) [{2}] into [{3}]",
                    loser.Title,
                    (PlatformFamily)loser.Platform,
                    loser.Path,
                    keeper.Path);

                RemoveEmptyFolder(loser.Path, keeper.Path);
            }
        }

        private void RemoveEmptyFolder(string path, string keeperPath)
        {
            if (path.IsNullOrWhiteSpace() || (keeperPath.IsNotNullOrWhiteSpace() && path.PathEquals(keeperPath)))
            {
                return;
            }

            try
            {
                if (_diskProvider.FolderExists(path) && _diskProvider.FolderEmpty(path))
                {
                    _diskProvider.DeleteFolder(path, false);
                    _logger.Info("Removed empty folder [{0}] left by a duplicate library entry", path);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Could not remove folder [{0}] left by a duplicate library entry", path);
            }
        }

        private static HashSet<int> ParseTags(string tags)
        {
            if (tags.IsNullOrWhiteSpace())
            {
                return new HashSet<int>();
            }

            try
            {
                return JsonSerializer.Deserialize<HashSet<int>>(tags) ?? new HashSet<int>();
            }
            catch (JsonException)
            {
                return new HashSet<int>();
            }
        }

        private List<DuplicateGameRow> ReadRows()
        {
            using var conn = _database.OpenConnection();

            return conn.Query<DuplicateGameRow>(@"SELECT ""Games"".""Id"", ""Games"".""Platform"", ""Games"".""Path"", ""Games"".""Monitored"",
                                                         ""Games"".""Tags"", ""Games"".""GameFileId"",
                                                         ""GameMetadata"".""IgdbId"", ""GameMetadata"".""SteamAppId"", ""GameMetadata"".""Title"",
                                                         (SELECT COUNT(*) FROM ""GameFiles"" WHERE ""GameFiles"".""GameId"" = ""Games"".""Id"") AS ""FileCount""
                                                  FROM ""Games""
                                                  JOIN ""GameMetadata"" ON ""GameMetadata"".""Id"" = ""Games"".""GameMetadataId""").ToList();
        }

        private class DuplicateGameRow
        {
            public int Id { get; set; }
            public int Platform { get; set; }
            public string Path { get; set; }
            public bool Monitored { get; set; }
            public string Tags { get; set; }
            public int GameFileId { get; set; }
            public int IgdbId { get; set; }
            public int SteamAppId { get; set; }
            public string Title { get; set; }
            public long FileCount { get; set; }
        }
    }
}
