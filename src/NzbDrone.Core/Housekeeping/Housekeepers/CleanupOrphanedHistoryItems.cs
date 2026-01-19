using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedHistoryItems : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedHistoryItems(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            CleanupOrphanedByGame();
        }

        private void CleanupOrphanedByGame()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""History""
                             WHERE ""Id"" IN (
                             SELECT ""History"".""Id"" FROM ""History""
                             LEFT OUTER JOIN ""Games""
                             ON ""History"".""GameId"" = ""Games"".""Id""
                             WHERE ""Games"".""Id"" IS NULL)");
        }
    }
}
