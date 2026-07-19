using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroCatalogEntry : ModelBase
    {
        public int CatalogSourceId { get; set; }
        public string SystemKey { get; set; }
        public string CanonicalName { get; set; }
        public string ParentCanonicalName { get; set; }
        public string CanonicalFileName { get; set; }
        public string ReleaseNumber { get; set; }
        public string NumberedCanonicalFileName { get; set; }
        public PlatformFamily PlatformFamily { get; set; }
    }
}
