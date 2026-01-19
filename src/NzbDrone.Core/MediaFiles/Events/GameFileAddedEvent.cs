using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameFileAddedEvent : IEvent
    {
        public GameFile GameFile { get; private set; }

        public GameFileAddedEvent(GameFile gameFile)
        {
            GameFile = gameFile;
        }
    }
}
