using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedSubtitleFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedSubtitleFiles(IMainDatabase database)
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
            mapper.Execute(@"DELETE FROM ""SubtitleFiles""
                             WHERE ""Id"" IN (
                             SELECT ""SubtitleFiles"".""Id"" FROM ""SubtitleFiles""
                             LEFT OUTER JOIN ""Games""
                             ON ""SubtitleFiles"".""GameId"" = ""Games"".""Id""
                             WHERE ""Games"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByGameFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SubtitleFiles""
                             WHERE ""Id"" IN (
                             SELECT ""SubtitleFiles"".""Id"" FROM ""SubtitleFiles""
                             LEFT OUTER JOIN ""GameFiles""
                             ON ""SubtitleFiles"".""GameFileId"" = ""GameFiles"".""Id""
                             WHERE ""SubtitleFiles"".""GameFileId"" > 0
                             AND ""GameFiles"".""Id"" IS NULL)");
        }
    }
}
