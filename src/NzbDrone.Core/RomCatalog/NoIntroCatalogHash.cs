using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroCatalogHash : ModelBase
    {
        public int CatalogEntryId { get; set; }
        public string HashType { get; set; }
        public string HashValue { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsBadDump { get; set; }
    }
}
