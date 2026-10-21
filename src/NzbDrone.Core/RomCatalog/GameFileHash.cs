using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.RomCatalog
{
    /// <summary>
    /// The No-Intro hash triplet of an imported game file, computed once and
    /// stored so catalog matching never has to read the file again (#205: the
    /// components endpoint hashed every file on every GET, 40-400 s for an 8 GB
    /// base file on NFS).
    /// </summary>
    public class GameFileHash : ModelBase
    {
        public int GameFileId { get; set; }

        // Invalidation key, together with GameFileId: the size and last-write
        // time the triplet was computed from. Either one moving means the file
        // was replaced and the stored hashes no longer describe it.
        public long Size { get; set; }

        // UTC ticks rather than a DateTime column because the comparison has to
        // be exact — a stale hash reported as current is worse than no hash —
        // and DateTime round-tripping through SQLite/Postgres is not tick-exact.
        public long LastModifiedUtcTicks { get; set; }

        public string Md5 { get; set; }
        public string Sha1 { get; set; }
        public string Crc32 { get; set; }
        public DateTime HashedAt { get; set; }

        public bool Matches(long size, DateTime lastModified)
        {
            return Size == size && LastModifiedUtcTicks == ToUtcTicks(lastModified);
        }

        public static long ToUtcTicks(DateTime lastModified)
        {
            return lastModified.ToUniversalTime().Ticks;
        }
    }
}
