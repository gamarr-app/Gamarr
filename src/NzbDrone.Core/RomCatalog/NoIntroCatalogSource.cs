using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroCatalogSource : ModelBase
    {
        public string Name { get; set; }
        public string SourceUrl { get; set; }
        public string PinnedRevision { get; set; }
        public string CatalogVersion { get; set; }
        public DateTime? LastSuccessfulSync { get; set; }
        public DateTime? LastAttemptedSync { get; set; }
        public string LastSyncError { get; set; }
    }
}
