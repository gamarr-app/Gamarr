using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroVerificationSnapshot : ModelBase
    {
        public int VerificationSetId { get; set; }
        public int CatalogSourceId { get; set; }
        public string CatalogRevision { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
