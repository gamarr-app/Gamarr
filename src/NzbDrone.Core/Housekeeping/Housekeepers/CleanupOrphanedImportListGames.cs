using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedImportListGames : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedImportListGames(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            CleanupOrphanedByImportLists();
            CleanupOrphanedByGameMetadata();
        }

        private void CleanupOrphanedByImportLists()
        {
            using var mapper = _database.OpenConnection();

            mapper.Execute(@"DELETE FROM ""ImportListGames""
                                 WHERE ""Id"" IN (
                                 SELECT ""ImportListGames"".""Id""
                                 FROM ""ImportListGames""
                                 LEFT OUTER JOIN ""ImportLists"" ON ""ImportListGames"".""ListId"" = ""ImportLists"".""Id""
                                 WHERE ""ImportLists"".""Id"" IS NULL)");
        }

        private void CleanupOrphanedByGameMetadata()
        {
            using var mapper = _database.OpenConnection();

            mapper.Execute(@"DELETE FROM ""ImportListGames""
                                 WHERE ""Id"" IN (
                                 SELECT ""ImportListGames"".""Id""
                                 FROM ""ImportListGames""
                                 LEFT OUTER JOIN ""GameMetadata"" ON ""ImportListGames"".""GameMetadataId"" = ""GameMetadata"".""Id""
                                 WHERE ""GameMetadata"".""Id"" IS NULL)");
        }
    }
}
