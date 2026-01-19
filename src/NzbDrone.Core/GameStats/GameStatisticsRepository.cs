using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.GameStats
{
    public interface IGameStatisticsRepository
    {
        List<GameStatistics> GameStatistics();
        List<GameStatistics> GameStatistics(int gameId);
    }

    public class GameStatisticsRepository : IGameStatisticsRepository
    {
        private const string _selectGamesTemplate = "SELECT /**select**/ FROM \"Games\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";
        private const string _selectGameFilesTemplate = "SELECT /**select**/ FROM \"GameFiles\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";

        private readonly IMainDatabase _database;

        public GameStatisticsRepository(IMainDatabase database)
        {
            _database = database;
        }

        public List<GameStatistics> GameStatistics()
        {
            return MapResults(Query(GamesBuilder(), _selectGamesTemplate),
                Query(GameFilesBuilder(), _selectGameFilesTemplate));
        }

        public List<GameStatistics> GameStatistics(int gameId)
        {
            return MapResults(Query(GamesBuilder().Where<Game>(x => x.Id == gameId), _selectGamesTemplate),
                Query(GameFilesBuilder().Where<GameFile>(x => x.GameId == gameId), _selectGameFilesTemplate));
        }

        private List<GameStatistics> MapResults(List<GameStatistics> gamesResult, List<GameStatistics> filesResult)
        {
            gamesResult.ForEach(e =>
            {
                var file = filesResult.SingleOrDefault(f => f.GameId == e.GameId);

                e.SizeOnDisk = file?.SizeOnDisk ?? 0;
                e.ReleaseGroupsString = file?.ReleaseGroupsString;
            });

            return gamesResult;
        }

        private List<GameStatistics> Query(SqlBuilder builder, string template)
        {
            var sql = builder.AddTemplate(template).LogQuery();

            using var conn = _database.OpenConnection();

            return conn.Query<GameStatistics>(sql.RawSql, sql.Parameters).ToList();
        }

        private SqlBuilder GamesBuilder()
        {
            return new SqlBuilder(_database.DatabaseType)
                .Select(@"""Games"".""Id"" AS GameId,
                        SUM(CASE WHEN ""GameFileId"" > 0 THEN 1 ELSE 0 END) AS GameFileCount")
                .GroupBy<Game>(x => x.Id);
        }

        private SqlBuilder GameFilesBuilder()
        {
            if (_database.DatabaseType == DatabaseType.SQLite)
            {
                return new SqlBuilder(_database.DatabaseType)
                    .Select(@"""GameId"",
                            SUM(COALESCE(""Size"", 0)) AS SizeOnDisk,
                            GROUP_CONCAT(""ReleaseGroup"", '|') AS ReleaseGroupsString")
                    .GroupBy<GameFile>(x => x.GameId);
            }

            return new SqlBuilder(_database.DatabaseType)
                .Select(@"""GameId"",
                        SUM(COALESCE(""Size"", 0)) AS SizeOnDisk,
                        string_agg(""ReleaseGroup"", '|') AS ReleaseGroupsString")
                .GroupBy<GameFile>(x => x.GameId);
        }
    }
}
