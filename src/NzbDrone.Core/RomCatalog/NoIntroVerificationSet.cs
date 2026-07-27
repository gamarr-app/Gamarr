using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroVerificationSet : ModelBase
    {
        public int CatalogSourceId { get; set; }
        public string SystemKey { get; set; }
        public string RootPath { get; set; }
        public bool Enabled { get; set; }
    }
}
