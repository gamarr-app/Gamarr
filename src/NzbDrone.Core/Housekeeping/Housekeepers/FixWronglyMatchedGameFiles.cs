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
            /*var mapper = _database.GetDataMapper();

            mapper.Execute(@"UPDATE ""Games""
                SET ""GameFileId"" =
                (Select ""Id"" FROM ""GameFiles"" WHERE ""Games"".""Id"" == ""GameFiles"".""GameId"")
                WHERE ""GameFileId"" !=
                (SELECT ""Id"" FROM ""GameFiles"" WHERE ""Games"".""Id"" == ""GameFiles"".""GameId"")");*/
        }
    }
}
