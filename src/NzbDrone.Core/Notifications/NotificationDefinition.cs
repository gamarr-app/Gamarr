using System;
using Equ;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public class NotificationDefinition : ProviderDefinition, IEquatable<NotificationDefinition>
    {
        private static readonly MemberwiseEqualityComparer<NotificationDefinition> Comparer = MemberwiseEqualityComparer<NotificationDefinition>.ByProperties;

        public bool OnGrab { get; set; }
        public bool OnDownload { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnGameAdded { get; set; }
        public bool OnGameDelete { get; set; }
        public bool OnGameFileDelete { get; set; }
        public bool OnGameFileDeleteForUpgrade { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool OnHealthRestored { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool OnManualInteractionRequired { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnGrab { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnDownload { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnUpgrade { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnRename { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnGameAdded { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnGameDelete { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnGameFileDelete { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnGameFileDeleteForUpgrade { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnHealthIssue { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnHealthRestored { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnApplicationUpdate { get; set; }

        [MemberwiseEqualityIgnore]
        public bool SupportsOnManualInteractionRequired { get; set; }

        [MemberwiseEqualityIgnore]
        public override bool Enable => OnGrab || OnDownload || (OnDownload && OnUpgrade) || OnRename || OnGameAdded || OnGameDelete || OnGameFileDelete || (OnGameFileDelete && OnGameFileDeleteForUpgrade) || OnHealthIssue || OnHealthRestored || OnApplicationUpdate || OnManualInteractionRequired;

        public bool Equals(NotificationDefinition other)
        {
            return Comparer.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NotificationDefinition);
        }

        public override int GetHashCode()
        {
            return Comparer.GetHashCode(this);
        }
    }
}
