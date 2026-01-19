using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedGameTranslations : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedGameTranslations(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""GameTranslations""
                             WHERE ""Id"" IN (
                             SELECT ""GameTranslations"".""Id"" FROM ""GameTranslations""
                             LEFT OUTER JOIN ""GameMetadata""
                             ON ""GameTranslations"".""GameMetadataId"" = ""GameMetadata"".""Id""
                             WHERE ""GameMetadata"".""Id"" IS NULL)");
        }
    }
}
