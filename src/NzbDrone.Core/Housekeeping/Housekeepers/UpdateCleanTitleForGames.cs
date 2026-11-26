using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    /// <summary>
    /// Recomputes stored clean titles with the current CleanGameTitle rules. Idempotent:
    /// only rows whose stored value differs are written, so it is a no-op once they agree.
    /// </summary>
    public class UpdateCleanTitleForGames : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public UpdateCleanTitleForGames(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            Refresh(mapper, "GameMetadata", "Title", "CleanTitle");
            Refresh(mapper, "GameMetadata", "OriginalTitle", "CleanOriginalTitle");
            Refresh(mapper, "AlternativeTitles", "Title", "CleanTitle");
            Refresh(mapper, "GameTranslations", "Title", "CleanTitle");
        }

        private static void Refresh(IDbConnection mapper, string table, string titleColumn, string cleanColumn)
        {
            var rows = mapper.Query($@"SELECT ""Id"", ""{titleColumn}"" AS ""Title"", ""{cleanColumn}"" AS ""Clean"" FROM ""{table}""
                                       WHERE ""{titleColumn}"" IS NOT NULL").ToList();

            var changed = new List<object>();

            foreach (var row in rows)
            {
                var clean = ((string)row.Title).CleanGameTitle();

                if (clean != (string)row.Clean)
                {
                    changed.Add(new { Id = (int)(long)row.Id, Clean = clean });
                }
            }

            if (changed.Any())
            {
                mapper.Execute($@"UPDATE ""{table}"" SET ""{cleanColumn}"" = @Clean WHERE ""Id"" = @Id", changed);
            }
        }
    }
}
