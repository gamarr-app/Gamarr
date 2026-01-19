using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedCollections : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedCollections(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""Collections"" WHERE ""IgdbId"" IN (SELECT ""X"".""IgdbId"" FROM (SELECT ""Collections"".""IgdbId"", COUNT(""Games"".""Id"") as ""GameCount"" FROM ""Collections""
                             LEFT OUTER JOIN ""GameMetadata"" ON ""Collections"".""IgdbId"" = ""GameMetadata"".""CollectionIgdbId""
                             LEFT OUTER JOIN ""Games"" ON ""Games"".""GameMetadataId"" = ""GameMetadata"".""Id""
                             GROUP BY ""Collections"".""Id"") AS ""X"" WHERE ""X"".""GameCount"" = 0)");
        }
    }
}
