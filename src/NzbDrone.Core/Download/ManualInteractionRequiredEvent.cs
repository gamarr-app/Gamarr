using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public class ManualInteractionRequiredEvent : IEvent
    {
        public RemoteGame RemoteGame { get; private set; }
        public TrackedDownload TrackedDownload { get; private set; }
        public GrabbedReleaseInfo Release { get; private set; }

        public ManualInteractionRequiredEvent(TrackedDownload trackedDownload, GrabbedReleaseInfo release)
        {
            TrackedDownload = trackedDownload;
            RemoteGame = trackedDownload.RemoteGame;
            Release = release;
        }
    }
}
