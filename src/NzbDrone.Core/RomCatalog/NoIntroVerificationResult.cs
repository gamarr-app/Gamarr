using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroVerificationResult : ModelBase
    {
        public int SnapshotId { get; set; }
        public int VerificationSetId { get; set; }
        public int? CatalogEntryId { get; set; }
        public string RelativePath { get; set; }
        public string ArchivePath { get; set; }
        public string MemberPath { get; set; }
        public string ActualFileName { get; set; }
        public string ExpectedFileName { get; set; }
        public string HashType { get; set; }
        public string HashValue { get; set; }
        public NoIntroVerificationStatus VerificationStatus { get; set; }
        public bool IsDuplicate { get; set; }
        public bool IsMissing { get; set; }
        public DateTime VerifiedAt { get; set; }
    }
}
