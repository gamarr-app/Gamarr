using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedGameFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedGameFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""GameFiles""
                             WHERE ""Id"" IN (
                             SELECT ""GameFiles"".""Id"" FROM ""GameFiles""
                             LEFT OUTER JOIN ""Games""
                             ON ""GameFiles"".""Id"" = ""Games"".""GameFileId""
                             WHERE ""Games"".""Id"" IS NULL)");
        }
    }
}
