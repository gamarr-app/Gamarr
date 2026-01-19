using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedGames : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedGames(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""Games""
                             WHERE ""Id"" IN (
                             SELECT ""Games"".""Id"" FROM ""Games""
                             LEFT OUTER JOIN ""GameMetadata"" ON ""Games"".""GameMetadataId"" = ""GameMetadata"".""Id""
                             WHERE ""GameMetadata"".""Id"" IS NULL)");
        }
    }
}
