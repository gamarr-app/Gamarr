using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameFolderCreatedEvent : IEvent
    {
        public Game Game { get; private set; }
        public GameFile GameFile { get; private set; }
        public string GameFileFolder { get; set; }
        public string GameFolder { get; set; }

        public GameFolderCreatedEvent(Game game, GameFile gameFile)
        {
            Game = game;
            GameFile = gameFile;
        }
    }
}
