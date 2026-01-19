using System;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Pending
{
    public class PendingRelease : ModelBase
    {
        public int GameId { get; set; }
        public string Title { get; set; }
        public DateTime Added { get; set; }
        public ParsedGameInfo ParsedGameInfo { get; set; }
        public ReleaseInfo Release { get; set; }
        public PendingReleaseReason Reason { get; set; }
        public PendingReleaseAdditionalInfo AdditionalInfo { get; set; }

        // Not persisted
        public RemoteGame RemoteGame { get; set; }
    }

    public class PendingReleaseAdditionalInfo
    {
        public GameMatchType GameMatchType { get; set; }
        public ReleaseSourceType ReleaseSource { get; set; }
    }
}
