using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameFileRenamedEvent : IEvent
    {
        public Game Game { get; private set; }
        public GameFile GameFile { get; private set; }
        public string OriginalPath { get; private set; }

        public GameFileRenamedEvent(Game game, GameFile gameFile, string originalPath)
        {
            Game = game;
            GameFile = gameFile;
            OriginalPath = originalPath;
        }
    }
}
