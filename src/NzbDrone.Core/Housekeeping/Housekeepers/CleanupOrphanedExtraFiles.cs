using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedExtraFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedExtraFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteOrphanedByGame();
            DeleteOrphanedByGameFile();
        }

        private void DeleteOrphanedByGame()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""ExtraFiles""
                             WHERE ""Id"" IN (
                             SELECT ""ExtraFiles"".""Id"" FROM ""ExtraFiles""
                             LEFT OUTER JOIN ""Games""
                             ON ""ExtraFiles"".""GameId"" = ""Games"".""Id""
                             WHERE ""Games"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByGameFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""ExtraFiles""
                             WHERE ""Id"" IN (
                             SELECT ""ExtraFiles"".""Id"" FROM ""ExtraFiles""
                             LEFT OUTER JOIN ""GameFiles""
                             ON ""ExtraFiles"".""GameFileId"" = ""GameFiles"".""Id""
                             WHERE ""ExtraFiles"".""GameFileId"" > 0
                             AND ""GameFiles"".""Id"" IS NULL)");
        }
    }
}
