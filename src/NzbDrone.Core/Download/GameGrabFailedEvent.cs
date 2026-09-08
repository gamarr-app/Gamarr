using NzbDrone.Common.Messaging;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    // Raised when a release passed every decision specification and then failed on the way to
    // the download client. Until this existed the only record of that was a log line, so a grab
    // could be approved and silently never happen.
    public class GameGrabFailedEvent : IEvent
    {
        public RemoteGame Game { get; private set; }
        public string Reason { get; private set; }

        public GameGrabFailedEvent(RemoteGame game, string reason)
        {
            Game = game;
            Reason = reason;
        }
    }
}
