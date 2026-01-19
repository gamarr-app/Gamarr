using NzbDrone.Common.Messaging;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public class GameGrabbedEvent : IEvent
    {
        public RemoteGame Game { get; private set; }
        public int DownloadClientId { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadClientName { get; set; }
        public string DownloadId { get; set; }

        public GameGrabbedEvent(RemoteGame game)
        {
            Game = game;
        }
    }
}
