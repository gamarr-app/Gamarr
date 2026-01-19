using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryRepository : IBasicRepository<GameHistory>
    {
        List<QualityModel> GetBestQualityInHistory(int gameId);
        GameHistory MostRecentForDownloadId(string downloadId);
        List<GameHistory> FindByDownloadId(string downloadId);
        List<GameHistory> FindDownloadHistory(int gameId, QualityModel quality);
        List<GameHistory> GetByGameId(int gameId, GameHistoryEventType? eventType);
        void DeleteForGames(List<int> gameIds);
        GameHistory MostRecentForGame(int gameId);
        List<GameHistory> Since(DateTime date, GameHistoryEventType? eventType);
        PagingSpec<GameHistory> GetPaged(PagingSpec<GameHistory> pagingSpec, int[] languages, int[] qualities);
    }

    public class HistoryRepository : BasicRepository<GameHistory>, IHistoryRepository
    {
        public HistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<QualityModel> GetBestQualityInHistory(int gameId)
        {
            var history = Query(x => x.GameId == gameId);

            return history.Select(h => h.Quality).ToList();
        }

        public GameHistory MostRecentForDownloadId(string downloadId)
        {
            return FindByDownloadId(downloadId).MaxBy(h => h.Date);
        }

        public List<GameHistory> FindByDownloadId(string downloadId)
        {
            return Query(x => x.DownloadId == downloadId);
        }

        public List<GameHistory> FindDownloadHistory(int gameId, QualityModel quality)
        {
            var allowed = new[] { (int)GameHistoryEventType.Grabbed, (int)GameHistoryEventType.DownloadFailed, (int)GameHistoryEventType.DownloadFolderImported };

            return Query(h => h.GameId == gameId &&
                         h.Quality == quality &&
                         allowed.Contains((int)h.EventType));
        }

        public List<GameHistory> GetByGameId(int gameId, GameHistoryEventType? eventType)
        {
            var builder = new SqlBuilder(_database.DatabaseType)
                .Join<GameHistory, Game>((h, m) => h.GameId == m.Id)
                .Join<Game, QualityProfile>((m, p) => m.QualityProfileId == p.Id)
                .Where<GameHistory>(h => h.GameId == gameId);

            if (eventType.HasValue)
            {
                builder.Where<GameHistory>(h => h.EventType == eventType);
            }

            return PagedQuery(builder).OrderByDescending(h => h.Date).ToList();
        }

        public void DeleteForGames(List<int> gameIds)
        {
            Delete(c => gameIds.Contains(c.GameId));
        }

        public GameHistory MostRecentForGame(int gameId)
        {
            return Query(x => x.GameId == gameId).MaxBy(h => h.Date);
        }

        public List<GameHistory> Since(DateTime date, GameHistoryEventType? eventType)
        {
            var builder = new SqlBuilder(_database.DatabaseType)
                .Join<GameHistory, Game>((h, m) => h.GameId == m.Id)
                .Join<Game, QualityProfile>((m, p) => m.QualityProfileId == p.Id)
                .Where<GameHistory>(x => x.Date >= date);

            if (eventType.HasValue)
            {
                builder.Where<GameHistory>(h => h.EventType == eventType);
            }

            return PagedQuery(builder).OrderBy(h => h.Date).ToList();
        }

        public PagingSpec<GameHistory> GetPaged(PagingSpec<GameHistory> pagingSpec, int[] languages, int[] qualities)
        {
            pagingSpec.Records = GetPagedRecords(PagedBuilder(languages, qualities), pagingSpec, PagedQuery);

            var countTemplate = $"SELECT COUNT(*) FROM (SELECT /**select**/ FROM \"{TableMapping.Mapper.TableNameMapping(typeof(GameHistory))}\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/) AS \"Inner\"";
            pagingSpec.TotalRecords = GetPagedRecordCount(PagedBuilder(languages, qualities).Select(typeof(GameHistory)), pagingSpec, countTemplate);

            return pagingSpec;
        }

        private SqlBuilder PagedBuilder(int[] languages, int[] qualities)
        {
            var builder = Builder()
                .Join<GameHistory, Game>((h, m) => h.GameId == m.Id)
                .Join<Game, QualityProfile>((m, p) => m.QualityProfileId == p.Id)
                .LeftJoin<Game, GameMetadata>((m, mm) => m.GameMetadataId == mm.Id);

            if (languages is { Length: > 0 })
            {
                builder.Where($"({BuildLanguageWhereClause(languages)})");
            }

            if (qualities is { Length: > 0 })
            {
                builder.Where($"({BuildQualityWhereClause(qualities)})");
            }

            return builder;
        }

        protected override IEnumerable<GameHistory> PagedQuery(SqlBuilder builder) =>
            _database.QueryJoined<GameHistory, Game, QualityProfile>(builder, (hist, game, profile) =>
            {
                hist.Game = game;
                hist.Game.QualityProfile = profile;
                return hist;
            });

        private string BuildLanguageWhereClause(int[] languages)
        {
            var clauses = new List<string>();

            foreach (var language in languages)
            {
                // There are 4 different types of values we should see:
                // - Not the last value in the array
                // - When it's the last value in the array and on different OSes
                // - When it was converted from a single language

                clauses.Add($"\"{TableMapping.Mapper.TableNameMapping(typeof(GameHistory))}\".\"Languages\" LIKE '[% {language},%]'");
                clauses.Add($"\"{TableMapping.Mapper.TableNameMapping(typeof(GameHistory))}\".\"Languages\" LIKE '[% {language}' || CHAR(13) || '%]'");
                clauses.Add($"\"{TableMapping.Mapper.TableNameMapping(typeof(GameHistory))}\".\"Languages\" LIKE '[% {language}' || CHAR(10) || '%]'");
                clauses.Add($"\"{TableMapping.Mapper.TableNameMapping(typeof(GameHistory))}\".\"Languages\" LIKE '[{language}]'");
            }

            return $"({string.Join(" OR ", clauses)})";
        }

        private string BuildQualityWhereClause(int[] qualities)
        {
            var clauses = new List<string>();

            foreach (var quality in qualities)
            {
                clauses.Add($"\"{TableMapping.Mapper.TableNameMapping(typeof(GameHistory))}\".\"Quality\" LIKE '%_quality_: {quality},%'");
            }

            return $"({string.Join(" OR ", clauses)})";
        }
    }
}
