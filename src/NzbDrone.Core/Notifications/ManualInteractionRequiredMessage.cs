using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Notifications
{
    public class ManualInteractionRequiredMessage
    {
        public string Message { get; set; }
        public Game Game { get; set; }
        public RemoteGame RemoteGame { get; set; }
        public TrackedDownload TrackedDownload { get; set; }
        public QualityModel Quality { get; set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; set; }
        public GrabbedReleaseInfo Release { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
