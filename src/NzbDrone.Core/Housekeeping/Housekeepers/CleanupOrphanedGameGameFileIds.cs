using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedGameGameFileIds : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedGameGameFileIds(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"UPDATE ""Games""
                             SET ""GameFileId"" = 0
                             WHERE ""Id"" IN (
                             SELECT ""Games"".""Id"" FROM ""Games""
                             LEFT OUTER JOIN ""GameFiles""
                             ON ""Games"".""GameFileId"" = ""GameFiles"".""Id""
                             WHERE ""GameFiles"".""Id"" IS NULL)");
        }
    }
}
