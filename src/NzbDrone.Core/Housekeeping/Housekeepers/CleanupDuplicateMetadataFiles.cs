using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupDuplicateMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupDuplicateMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteDuplicateGameMetadata();
            DeleteDuplicateGameFileMetadata();
        }

        // Game-level duplicates only. Metadata is written per GameFile, so a game with
        // an update or DLC legitimately has one GameMetadata row per unit; grouping
        // those by GameId alone deleted a real unit's row on every run. Rows that name
        // a file are covered by DeleteDuplicateGameFileMetadata.
        private void DeleteDuplicateGameMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" = 1
                                 AND COALESCE(""GameFileId"", 0) = 0
                                 GROUP BY ""GameId"", ""Consumer""
                                 HAVING COUNT(""GameId"") > 1
                             )");
        }

        private void DeleteDuplicateGameFileMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" = 1
                                 AND COALESCE(""GameFileId"", 0) > 0
                                 GROUP BY ""GameFileId"", ""Consumer""
                                 HAVING COUNT(""GameFileId"") > 1
                             )");
        }
    }
}
