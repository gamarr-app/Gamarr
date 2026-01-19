using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupDuplicateGameTranslations : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupDuplicateGameTranslations(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            mapper.Execute(@"DELETE FROM ""GameTranslations""
            WHERE ""Id"" IN (
                SELECT MAX(""Id"") FROM ""GameTranslations""
                GROUP BY ""GameMetadataId"", ""Language""
                HAVING COUNT(""Id"") > 1
            )");
        }
    }
}
