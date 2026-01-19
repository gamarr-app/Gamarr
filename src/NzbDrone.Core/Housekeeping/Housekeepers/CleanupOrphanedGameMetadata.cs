using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedGameMetadata : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedGameMetadata(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""GameMetadata""
                             WHERE ""Id"" IN (
                             SELECT ""GameMetadata"".""Id"" FROM ""GameMetadata""
                             LEFT OUTER JOIN ""Games"" ON ""Games"".""GameMetadataId"" = ""GameMetadata"".""Id""
                             LEFT OUTER JOIN ""Collections"" ON ""Collections"".""IgdbId"" = ""GameMetadata"".""CollectionIgdbId""
                             LEFT OUTER JOIN ""ImportListGames"" ON ""ImportListGames"".""GameMetadataId"" = ""GameMetadata"".""Id""
                             WHERE ""Games"".""Id"" IS NULL AND ""ImportListGames"".""Id"" IS NULL AND ""Collections"".""Id"" IS NULL)");
        }
    }
}
