using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedGameFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedGameFiles(IMainDatabase database)
        {
            _database = database;
        }

        // A file row is orphaned only when the GAME it belongs to is gone.
        //
        // This used to join ""GameFiles"".""Id"" = ""Games"".""GameFileId"", the
        // Radarr one-file-per-movie rule: anything no game pointed at as its
        // PRIMARY file counted as an orphan. Since #149 a game owns several rows
        // (base + ""Updates/<ver>"" + ""DLC/<name>"") and only the base is ever
        // named by Games.GameFileId, so every alongside unit was deleted on every
        // housekeeping run — silently, with no history event, while the files sat
        // untouched on disk (#221). Components then read as missing, RSS/search
        // re-wanted installed updates, and the next scan re-adopted the same
        // folders.
        //
        // GameFiles.GameId is NOT NULL and indexed since migration 001, so the
        // join below is the cheap one. A row whose GameId matches no game — including
        // the 0 sentinel for an unassigned row — is a genuine orphan: nothing can
        // reach it (every read path keys off GameId) and no game can ever claim it
        // again, so it is deleted deliberately.
        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""GameFiles""
                             WHERE ""Id"" IN (
                             SELECT ""GameFiles"".""Id"" FROM ""GameFiles""
                             LEFT OUTER JOIN ""Games""
                             ON ""GameFiles"".""GameId"" = ""Games"".""Id""
                             WHERE ""Games"".""Id"" IS NULL)");
        }
    }
}
