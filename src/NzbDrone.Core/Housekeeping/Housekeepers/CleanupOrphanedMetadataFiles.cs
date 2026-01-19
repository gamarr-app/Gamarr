using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteOrphanedByGame();
            DeleteOrphanedByGameFile();
            DeleteWhereGameFileIsZero();
        }

        private void DeleteOrphanedByGame()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""Games""
                             ON ""MetadataFiles"".""GameId"" = ""Games"".""Id""
                             WHERE ""Games"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByGameFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""GameFiles""
                             ON ""MetadataFiles"".""GameFileId"" = ""GameFiles"".""Id""
                             WHERE ""MetadataFiles"".""GameFileId"" > 0
                             AND ""GameFiles"".""Id"" IS NULL)");
        }

        private void DeleteWhereGameFileIsZero()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""Id"" FROM ""MetadataFiles""
                             WHERE ""Type"" IN (1, 2)
                             AND ""GameFileId"" = 0)");
        }
    }
}
