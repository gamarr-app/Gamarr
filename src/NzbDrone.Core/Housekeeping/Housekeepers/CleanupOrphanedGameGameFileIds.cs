using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedGameGameFileIds : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedGameGameFileIds(IMainDatabase database)
        {
            _database = database;
        }

        // Games.GameFileId names the game's PRIMARY (base) file row. Repair the
        // ones that name a row which no longer exists.
        //
        // Zeroing the pointer unconditionally was fine when a game owned exactly
        // one file. With N rows per game (#149) it leaves the game reading as
        // file-less (Game.HasFile is just GameFileId > 0 — the pointer, never a
        // disk test, #216) while its base file row is still there, which is how
        // #221 kept showing games as missing between a housekeeping run and the
        // next disk scan. So re-anchor at a surviving BASE row when there is one
        // and only fall back to 0 when there isn't.
        //
        // "Base" here matches GameFileUnit.IsBase: not under "Updates/" or "DLC/"
        // and not linked to a component slot. An empty/NULL RelativePath is a
        // folder-granularity base release and must still qualify. Never anchor at
        // an update or DLC row — that would claim a base game that is not on disk.
        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"UPDATE ""Games""
                             SET ""GameFileId"" = COALESCE((
                                 SELECT MIN(""GameFiles"".""Id"") FROM ""GameFiles""
                                 WHERE ""GameFiles"".""GameId"" = ""Games"".""Id""
                                 AND ""GameFiles"".""ComponentId"" = 0
                                 AND COALESCE(REPLACE(""GameFiles"".""RelativePath"", '\', '/'), '') NOT LIKE 'Updates/%'
                                 AND COALESCE(REPLACE(""GameFiles"".""RelativePath"", '\', '/'), '') NOT LIKE 'DLC/%'), 0)
                             WHERE ""Id"" IN (
                             SELECT ""Games"".""Id"" FROM ""Games""
                             LEFT OUTER JOIN ""GameFiles""
                             ON ""Games"".""GameFileId"" = ""GameFiles"".""Id""
                             WHERE ""GameFiles"".""Id"" IS NULL)");
        }
    }
}
