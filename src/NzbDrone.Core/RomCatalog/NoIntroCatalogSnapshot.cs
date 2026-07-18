using System.Collections.Generic;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroCatalogSnapshot
    {
        public string CatalogVersion { get; set; }
        public string SystemKey { get; set; }
        public List<NoIntroCatalogSnapshotEntry> Entries { get; set; } = new List<NoIntroCatalogSnapshotEntry>();
    }

    public class NoIntroCatalogSnapshotEntry
    {
        public string CanonicalName { get; set; }
        public string CanonicalFileName { get; set; }
        public List<NoIntroCatalogSnapshotHash> Hashes { get; set; } = new List<NoIntroCatalogSnapshotHash>();
    }

    public class NoIntroCatalogSnapshotHash
    {
        public string HashType { get; set; }
        public string HashValue { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsBadDump { get; set; }
    }
}
