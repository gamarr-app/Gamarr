using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameFileUpdatedEvent : IEvent
    {
        public GameFile GameFile { get; private set; }

        public GameFileUpdatedEvent(GameFile gameFile)
        {
            GameFile = gameFile;
        }
    }
}
