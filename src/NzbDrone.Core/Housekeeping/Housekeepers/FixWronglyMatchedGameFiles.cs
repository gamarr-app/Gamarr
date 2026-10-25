using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class FixWronglyMatchedGameFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public FixWronglyMatchedGameFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            // Deliberately a no-op, and do NOT revive the SQL below as written: it
            // assumes one file row per game, so with base + "Updates/<ver>" +
            // "DLC/<name>" rows (#149) the scalar subqueries match several rows and
            // would repoint Games.GameFileId at an arbitrary unit (#221).
            /*var mapper = _database.GetDataMapper();

            mapper.Execute(@"UPDATE ""Games""
                SET ""GameFileId"" =
                (Select ""Id"" FROM ""GameFiles"" WHERE ""Games"".""Id"" == ""GameFiles"".""GameId"")
                WHERE ""GameFileId"" !=
                (SELECT ""Id"" FROM ""GameFiles"" WHERE ""Games"".""Id"" == ""GameFiles"".""GameId"")");*/
        }
    }
}
