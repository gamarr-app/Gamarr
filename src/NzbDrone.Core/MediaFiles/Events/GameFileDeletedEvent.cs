using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameFileDeletedEvent : IEvent
    {
        public GameFile GameFile { get; private set; }
        public DeleteMediaFileReason Reason { get; private set; }

        public GameFileDeletedEvent(GameFile gameFile, DeleteMediaFileReason reason)
        {
            GameFile = gameFile;
            Reason = reason;
        }
    }
}
