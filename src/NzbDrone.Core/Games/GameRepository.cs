using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Games
{
    public interface IGameRepository : IBasicRepository<Game>
    {
        bool GamePathExists(string path);
        List<Game> FindByTitles(List<string> titles);

        // Primary identifier - Steam App ID
        Game FindBySteamAppId(int steamAppId);
        List<Game> FindBySteamAppId(List<int> steamAppIds);
        List<int> AllGameSteamAppIds();

        // Secondary identifiers
        Game FindByIgdbId(int igdbid);
        List<Game> FindByIgdbId(List<int> igdbids);
        List<int> AllGameIgdbIds();
        Game FindByRawgId(int rawgId);

        List<Game> GamesBetweenDates(DateTime start, DateTime end, bool includeUnmonitored);
        PagingSpec<Game> GamesWithoutFiles(PagingSpec<Game> pagingSpec);
        List<Game> GetGamesByFileId(int fileId);
        List<Game> GetGamesByCollectionIgdbId(int collectionId);
        PagingSpec<Game> GamesWhereCutoffUnmet(PagingSpec<Game> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff);
        Game FindByPath(string path);
        Dictionary<int, string> AllGamePaths();
        Dictionary<int, List<int>> AllGameTags();
        List<int> GetRecommendations();
        List<int> GetRawgRecommendations();
        bool ExistsByMetadataId(int metadataId);
        HashSet<int> AllGameWithCollectionsIgdbIds();

        // DLC-related methods
        List<Game> GetDlcsForGame(int parentIgdbId);
        Game GetParentGame(int parentIgdbId);
        List<Game> GetAllDlcs();
        List<Game> GetMainGamesOnly();
    }

    public class GameRepository : BasicRepository<Game>, IGameRepository
    {
        private readonly IQualityProfileRepository _profileRepository;
        private readonly IAlternativeTitleRepository _alternativeTitleRepository;

        public GameRepository(IMainDatabase database,
                               IQualityProfileRepository profileRepository,
                               IAlternativeTitleRepository alternativeTitleRepository,
                               IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
            _profileRepository = profileRepository;
            _alternativeTitleRepository = alternativeTitleRepository;
        }

        protected override IEnumerable<Game> PagedQuery(SqlBuilder builder) =>
            _database.QueryJoined<Game, GameMetadata>(builder, (game, gameMetadata) =>
            {
                game.GameMetadata = gameMetadata;
                return game;
            });

        protected override SqlBuilder Builder() => new SqlBuilder(_database.DatabaseType)
            .Join<Game, QualityProfile>((m, p) => m.QualityProfileId == p.Id)
            .Join<Game, GameMetadata>((m, p) => m.GameMetadataId == p.Id)
            .LeftJoin<Game, GameFile>((m, f) => m.Id == f.GameId)
            .LeftJoin<GameMetadata, AlternativeTitle>((mm, t) => mm.Id == t.GameMetadataId);

        private Game Map(Dictionary<int, Game> dict, Game game, GameMetadata metadata, QualityProfile qualityProfile, GameFile gameFile, AlternativeTitle altTitle = null, GameTranslation translation = null)
        {
            if (!dict.TryGetValue(game.Id, out var gameEntry))
            {
                gameEntry = game;
                gameEntry.GameMetadata = metadata;
                gameEntry.QualityProfile = qualityProfile;
                gameEntry.GameFile = gameFile;
                dict.Add(gameEntry.Id, gameEntry);
            }

            if (altTitle != null)
            {
                gameEntry.GameMetadata.Value.AlternativeTitles.Add(altTitle);
            }

            if (translation != null)
            {
                gameEntry.GameMetadata.Value.Translations.Add(translation);
            }

            return gameEntry;
        }

        protected override List<Game> Query(SqlBuilder builder)
        {
            var gameDictionary = new Dictionary<int, Game>();

            _ = _database.QueryJoined<Game, GameMetadata, QualityProfile, GameFile, AlternativeTitle>(
                builder,
                (game, metadata, qualityProfile, file, altTitle) => Map(gameDictionary, game, metadata, qualityProfile, file, altTitle));

            return gameDictionary.Values.ToList();
        }

        public override IEnumerable<Game> All()
        {
            // the skips the join on profile and alternative title and populates manually
            // to avoid repeatedly deserializing the same profile / game
            var builder = new SqlBuilder(_database.DatabaseType)
                .LeftJoin<Game, GameMetadata>((m, f) => m.GameMetadataId == f.Id)
                .LeftJoin<Game, GameFile>((m, f) => m.GameFileId == f.Id);

            var qualityProfiles = _profileRepository.All().ToDictionary(x => x.Id);
            var alternativeTitles = _alternativeTitleRepository.All()
                .GroupBy(x => x.GameMetadataId)
                .ToDictionary(x => x.Key, y => y.ToList());

            return _database.QueryJoined<Game, GameMetadata, GameFile>(
                builder,
                (game, metadata, file) =>
                {
                    game.GameMetadata = metadata;
                    game.GameFile = file;
                    game.QualityProfile = qualityProfiles[game.QualityProfileId];

                    if (alternativeTitles.TryGetValue(game.GameMetadataId, out var altTitles))
                    {
                        game.GameMetadata.Value.AlternativeTitles = altTitles;
                    }

                    return game;
                });
        }

        public bool GamePathExists(string path)
        {
            return Query(x => x.Path == path).Any();
        }

        public List<Game> FindByTitles(List<string> titles)
        {
            var distinct = titles.Distinct().ToList();

            var results = new List<Game>();

            results.AddRange(FindByGameTitles(distinct));
            results.AddRange(FindByAltTitles(distinct));
            results.AddRange(FindByTransTitles(distinct));

            return results.DistinctBy(x => x.Id).ToList();
        }

        // This is a bit of a hack, but if you try to combine / rationalise these then
        // SQLite makes a mess of the query plan and ends up doing a table scan
        private List<Game> FindByGameTitles(List<string> titles)
        {
            var gameDictionary = new Dictionary<int, Game>();

            var builder = new SqlBuilder(_database.DatabaseType)
                .Join<Game, QualityProfile>((m, p) => m.QualityProfileId == p.Id)
                .Join<Game, GameMetadata>((m, p) => m.GameMetadataId == p.Id)
                .LeftJoin<Game, GameFile>((m, f) => m.Id == f.GameId)
                .Where<GameMetadata>(x => titles.Contains(x.CleanTitle) || titles.Contains(x.CleanOriginalTitle));

            _ = _database.QueryJoined<Game, GameMetadata, QualityProfile, GameFile>(
                builder,
                (game, metadata, qualityProfile, file) => Map(gameDictionary, game, metadata, qualityProfile, file));

            return gameDictionary.Values.ToList();
        }

        private List<Game> FindByAltTitles(List<string> titles)
        {
            var gameDictionary = new Dictionary<int, Game>();

            var builder = new SqlBuilder(_database.DatabaseType)
                .Join<AlternativeTitle, GameMetadata>((t, mm) => t.GameMetadataId == mm.Id)
                .Join<GameMetadata, Game>((mm, m) => mm.Id == m.GameMetadataId)
                .Join<Game, QualityProfile>((m, p) => m.QualityProfileId == p.Id)
                .LeftJoin<Game, GameFile>((m, f) => m.Id == f.GameId)
                .Where<AlternativeTitle>(x => titles.Contains(x.CleanTitle));

            _ = _database.QueryJoined<AlternativeTitle, QualityProfile, Game, GameMetadata, GameFile>(
                builder,
                (altTitle, qualityProfile, game, metadata, file) =>
                {
                    _ = Map(gameDictionary, game, metadata, qualityProfile, file, altTitle);
                    return null;
                });

            return gameDictionary.Values.ToList();
        }

        private List<Game> FindByTransTitles(List<string> titles)
        {
            var gameDictionary = new Dictionary<int, Game>();

            var builder = new SqlBuilder(_database.DatabaseType)
                .Join<GameTranslation, GameMetadata>((t, mm) => t.GameMetadataId == mm.Id)
                .Join<GameMetadata, Game>((mm, m) => mm.Id == m.GameMetadataId)
                .Join<Game, QualityProfile>((m, p) => m.QualityProfileId == p.Id)
                .LeftJoin<Game, GameFile>((m, f) => m.Id == f.GameId)
                .Where<GameTranslation>(x => titles.Contains(x.CleanTitle));

            _ = _database.QueryJoined<GameTranslation, QualityProfile, Game, GameMetadata, GameFile>(
                builder,
                (trans, qualityProfile, game, metadata, file) =>
                {
                    _ = Map(gameDictionary, game, metadata, qualityProfile, file, null, trans);
                    return null;
                });

            return gameDictionary.Values.ToList();
        }

        public Game FindByIgdbId(int igdbid)
        {
            return Query(x => x.GameMetadata.Value.IgdbId == igdbid).FirstOrDefault();
        }

        public List<Game> FindByIgdbId(List<int> igdbids)
        {
            return Query(x => igdbids.Contains(x.IgdbId));
        }

        // Primary identifier - Steam App ID
        public Game FindBySteamAppId(int steamAppId)
        {
            return Query(x => x.GameMetadata.Value.SteamAppId == steamAppId).FirstOrDefault();
        }

        public List<Game> FindBySteamAppId(List<int> steamAppIds)
        {
            return Query(x => steamAppIds.Contains(x.SteamAppId));
        }

        public List<int> AllGameSteamAppIds()
        {
            using (var conn = _database.OpenConnection())
            {
                return conn.Query<int>("SELECT \"SteamAppId\" FROM \"GameMetadata\" JOIN \"Games\" ON (\"Games\".\"GameMetadataId\" = \"GameMetadata\".\"Id\") WHERE \"SteamAppId\" > 0").ToList();
            }
        }

        // Secondary identifier - RAWG ID
        public Game FindByRawgId(int rawgId)
        {
            return Query(x => x.GameMetadata.Value.RawgId == rawgId).FirstOrDefault();
        }

        public List<Game> GetGamesByFileId(int fileId)
        {
            return Query(x => x.GameFileId == fileId);
        }

        public List<Game> GetGamesByCollectionIgdbId(int collectionId)
        {
            return Query(x => x.GameMetadata.Value.CollectionIgdbId == collectionId);
        }

        public List<Game> GamesBetweenDates(DateTime start, DateTime end, bool includeUnmonitored)
        {
            var builder = Builder()
                .Where<Game>(m =>
                              (m.GameMetadata.Value.EarlyAccess >= start && m.GameMetadata.Value.EarlyAccess <= end) ||
                              (m.GameMetadata.Value.PhysicalRelease >= start && m.GameMetadata.Value.PhysicalRelease <= end) ||
                              (m.GameMetadata.Value.DigitalRelease >= start && m.GameMetadata.Value.DigitalRelease <= end));

            if (!includeUnmonitored)
            {
                builder.Where<Game>(x => x.Monitored == true);
            }

            return Query(builder);
        }

        public SqlBuilder GamesWithoutFilesBuilder() => Builder()
            .Where<Game>(x => x.GameFileId == 0)
            .Where<Game>(m => m.GameMetadata.Value.Year > 0)
            .GroupBy<Game>(m => m.Id)
            .GroupBy<GameMetadata>(m => m.Id);

        public PagingSpec<Game> GamesWithoutFiles(PagingSpec<Game> pagingSpec)
        {
            pagingSpec.Records = GetPagedRecords(GamesWithoutFilesBuilder(), pagingSpec, PagedQuery);
            pagingSpec.TotalRecords = GetPagedRecordCount(GamesWithoutFilesBuilder().SelectCountDistinct<Game>(x => x.Id), pagingSpec);

            return pagingSpec;
        }

        public SqlBuilder GamesWhereCutoffUnmetBuilder(List<QualitiesBelowCutoff> qualitiesBelowCutoff) => Builder()
            .Where<Game>(x => x.GameFileId != 0)
            .Where(BuildQualityCutoffWhereClause(qualitiesBelowCutoff))
            .GroupBy<Game>(m => m.Id)
            .GroupBy<GameMetadata>(m => m.Id);

        public PagingSpec<Game> GamesWhereCutoffUnmet(PagingSpec<Game> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            pagingSpec.Records = GetPagedRecords(GamesWhereCutoffUnmetBuilder(qualitiesBelowCutoff), pagingSpec, PagedQuery);
            pagingSpec.TotalRecords = GetPagedRecordCount(GamesWhereCutoffUnmetBuilder(qualitiesBelowCutoff).SelectCountDistinct<Game>(x => x.Id), pagingSpec);

            return pagingSpec;
        }

        private string BuildQualityCutoffWhereClause(List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            var clauses = new List<string>();

            foreach (var profile in qualitiesBelowCutoff)
            {
                foreach (var belowCutoff in profile.QualityIds)
                {
                    clauses.Add(string.Format($"(\"{_table}\".\"QualityProfileId\" = {profile.ProfileId} AND \"GameFiles\".\"Quality\" LIKE '%_quality_: {belowCutoff},%')"));
                }
            }

            return string.Format("({0})", string.Join(" OR ", clauses));
        }

        public Game FindByPath(string path)
        {
            return Query(x => x.Path == path).FirstOrDefault();
        }

        public Dictionary<int, string> AllGamePaths()
        {
            using (var conn = _database.OpenConnection())
            {
                var strSql = "SELECT \"Id\" AS \"Key\", \"Path\" AS \"Value\" FROM \"Games\"";
                return conn.Query<KeyValuePair<int, string>>(strSql).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public List<int> AllGameIgdbIds()
        {
            using (var conn = _database.OpenConnection())
            {
                return conn.Query<int>("SELECT \"IgdbId\" FROM \"GameMetadata\" JOIN \"Games\" ON (\"Games\".\"GameMetadataId\" = \"GameMetadata\".\"Id\")").ToList();
            }
        }

        public Dictionary<int, List<int>> AllGameTags()
        {
            using (var conn = _database.OpenConnection())
            {
                var strSql = "SELECT \"Id\" AS \"Key\", \"Tags\" AS \"Value\" FROM \"Games\" WHERE \"Tags\" IS NOT NULL";
                return conn.Query<KeyValuePair<int, List<int>>>(strSql).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public List<int> GetRecommendations()
        {
            var recommendations = new List<int>();

            if (_database.DatabaseType == DatabaseType.SQLite && _database.Version < new Version("3.9.0"))
            {
                return recommendations;
            }

            using (var conn = _database.OpenConnection())
            {
                if (_database.DatabaseType == DatabaseType.PostgreSQL)
                {
                    recommendations = conn.Query<int>(@"SELECT DISTINCT ""Rec"" FROM (
                                                    SELECT DISTINCT ""Rec"" FROM
                                                    (
                                                    SELECT DISTINCT CAST(""value"" AS INT) AS ""Rec"" FROM ""GameMetadata"" JOIN ""Games"" ON ""Games"".""GameMetadataId"" = ""GameMetadata"".""Id"", json_array_elements_text((""GameMetadata"".""IgdbRecommendations"")::json)
                                                    WHERE CAST(""value"" AS INT) NOT IN (SELECT ""IgdbId"" FROM ""GameMetadata"" union SELECT ""IgdbId"" from ""ImportExclusions"" as sub1) LIMIT 10
                                                    ) as sub2
                                                    UNION
                                                    SELECT ""Rec"" FROM
                                                    (
                                                    SELECT CAST(""value"" AS INT) AS ""Rec"" FROM ""GameMetadata"" JOIN ""Games"" ON ""Games"".""GameMetadataId"" = ""GameMetadata"".""Id"", json_array_elements_text((""GameMetadata"".""IgdbRecommendations"")::json)
                                                    WHERE CAST(""value"" AS INT) NOT IN (SELECT ""IgdbId"" FROM ""GameMetadata"" union SELECT ""IgdbId"" from ""ImportExclusions"" as sub2)
                                                    GROUP BY ""Rec"" ORDER BY count(*) DESC LIMIT 120
                                                    ) as sub4
                                                    ) as sub5
                                                    LIMIT 100;").ToList();
                }
                else
                {
                    recommendations = conn.Query<int>(@"SELECT DISTINCT ""Rec"" FROM (
                                                    SELECT DISTINCT ""Rec"" FROM
                                                    (
                                                    SELECT DISTINCT CAST(""j"".""value"" AS INT) AS ""Rec"" FROM ""GameMetadata"" JOIN ""Games"" ON ""Games"".""GameMetadataId"" == ""GameMetadata"".""Id""
                                                    CROSS JOIN json_each(""GameMetadata"".""IgdbRecommendations"") AS ""j""
                                                    WHERE ""Rec"" NOT IN (SELECT ""IgdbId"" FROM ""GameMetadata"" union SELECT ""IgdbId"" from ""ImportExclusions"") LIMIT 10
                                                    )
                                                    UNION
                                                    SELECT ""Rec"" FROM
                                                    (
                                                    SELECT CAST(""j"".""value"" AS INT) AS ""Rec"" FROM ""GameMetadata"" JOIN ""Games"" ON ""Games"".""GameMetadataId"" == ""GameMetadata"".""Id""
                                                    CROSS JOIN json_each(""GameMetadata"".""IgdbRecommendations"") AS ""j""
                                                    WHERE ""Rec"" NOT IN (SELECT ""IgdbId"" FROM ""GameMetadata"" union SELECT ""IgdbId"" from ""ImportExclusions"")
                                                    GROUP BY ""Rec"" ORDER BY count(*) DESC LIMIT 120
                                                    )
                                                    )
                                                    LIMIT 100;").ToList();
                }
            }

            return recommendations;
        }

        public List<int> GetRawgRecommendations()
        {
            var recommendations = new List<int>();

            if (_database.DatabaseType == DatabaseType.SQLite && _database.Version < new Version("3.9.0"))
            {
                return recommendations;
            }

            using (var conn = _database.OpenConnection())
            {
                if (_database.DatabaseType == DatabaseType.PostgreSQL)
                {
                    recommendations = conn.Query<int>(@"SELECT DISTINCT ""Rec"" FROM (
                                                    SELECT DISTINCT ""Rec"" FROM
                                                    (
                                                    SELECT DISTINCT CAST(""value"" AS INT) AS ""Rec"" FROM ""GameMetadata"" JOIN ""Games"" ON ""Games"".""GameMetadataId"" = ""GameMetadata"".""Id"", json_array_elements_text((""GameMetadata"".""RawgRecommendations"")::json)
                                                    WHERE CAST(""value"" AS INT) NOT IN (SELECT ""RawgId"" FROM ""GameMetadata"" WHERE ""RawgId"" > 0 union SELECT ""RawgId"" from ""ImportExclusions"" WHERE ""RawgId"" > 0 as sub1) LIMIT 10
                                                    ) as sub2
                                                    UNION
                                                    SELECT ""Rec"" FROM
                                                    (
                                                    SELECT CAST(""value"" AS INT) AS ""Rec"" FROM ""GameMetadata"" JOIN ""Games"" ON ""Games"".""GameMetadataId"" = ""GameMetadata"".""Id"", json_array_elements_text((""GameMetadata"".""RawgRecommendations"")::json)
                                                    WHERE CAST(""value"" AS INT) NOT IN (SELECT ""RawgId"" FROM ""GameMetadata"" WHERE ""RawgId"" > 0 union SELECT ""RawgId"" from ""ImportExclusions"" WHERE ""RawgId"" > 0 as sub2)
                                                    GROUP BY ""Rec"" ORDER BY count(*) DESC LIMIT 120
                                                    ) as sub4
                                                    ) as sub5
                                                    LIMIT 100;").ToList();
                }
                else
                {
                    recommendations = conn.Query<int>(@"SELECT DISTINCT ""Rec"" FROM (
                                                    SELECT DISTINCT ""Rec"" FROM
                                                    (
                                                    SELECT DISTINCT CAST(""j"".""value"" AS INT) AS ""Rec"" FROM ""GameMetadata"" JOIN ""Games"" ON ""Games"".""GameMetadataId"" == ""GameMetadata"".""Id""
                                                    CROSS JOIN json_each(""GameMetadata"".""RawgRecommendations"") AS ""j""
                                                    WHERE ""Rec"" NOT IN (SELECT ""RawgId"" FROM ""GameMetadata"" WHERE ""RawgId"" > 0 union SELECT ""RawgId"" from ""ImportExclusions"" WHERE ""RawgId"" > 0) LIMIT 10
                                                    )
                                                    UNION
                                                    SELECT ""Rec"" FROM
                                                    (
                                                    SELECT CAST(""j"".""value"" AS INT) AS ""Rec"" FROM ""GameMetadata"" JOIN ""Games"" ON ""Games"".""GameMetadataId"" == ""GameMetadata"".""Id""
                                                    CROSS JOIN json_each(""GameMetadata"".""RawgRecommendations"") AS ""j""
                                                    WHERE ""Rec"" NOT IN (SELECT ""RawgId"" FROM ""GameMetadata"" WHERE ""RawgId"" > 0 union SELECT ""RawgId"" from ""ImportExclusions"" WHERE ""RawgId"" > 0)
                                                    GROUP BY ""Rec"" ORDER BY count(*) DESC LIMIT 120
                                                    )
                                                    )
                                                    LIMIT 100;").ToList();
                }
            }

            return recommendations;
        }

        public bool ExistsByMetadataId(int metadataId)
        {
            return Query(x => x.GameMetadataId == metadataId).Any();
        }

        public HashSet<int> AllGameWithCollectionsIgdbIds()
        {
            using var conn = _database.OpenConnection();

            return conn.Query<int>("SELECT \"IgdbId\" FROM \"GameMetadata\" JOIN \"Games\" ON (\"Games\".\"GameMetadataId\" = \"GameMetadata\".\"Id\") WHERE \"CollectionIgdbId\" > 0").ToHashSet();
        }

        public List<Game> GetDlcsForGame(int parentIgdbId)
        {
            return Query(x => x.GameMetadata.Value.ParentGameId == parentIgdbId);
        }

        public Game GetParentGame(int parentIgdbId)
        {
            return Query(x => x.GameMetadata.Value.IgdbId == parentIgdbId).FirstOrDefault();
        }

        public List<Game> GetAllDlcs()
        {
            return Query(x =>
                x.GameMetadata.Value.GameType == GameType.DlcAddon ||
                x.GameMetadata.Value.GameType == GameType.Expansion ||
                x.GameMetadata.Value.GameType == GameType.StandaloneExpansion ||
                x.GameMetadata.Value.GameType == GameType.Episode ||
                x.GameMetadata.Value.GameType == GameType.Season ||
                x.GameMetadata.Value.GameType == GameType.Pack);
        }

        public List<Game> GetMainGamesOnly()
        {
            return Query(x =>
                x.GameMetadata.Value.GameType == GameType.MainGame ||
                x.GameMetadata.Value.GameType == GameType.Remake ||
                x.GameMetadata.Value.GameType == GameType.Remaster ||
                x.GameMetadata.Value.GameType == GameType.ExpandedGame ||
                x.GameMetadata.Value.GameType == GameType.Port);
        }
    }
}
