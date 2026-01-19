using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameScannedEvent : IEvent
    {
        public Game Game { get; private set; }
        public List<string> PossibleExtraFiles { get; set; }

        public GameScannedEvent(Game game, List<string> possibleExtraFiles)
        {
            Game = game;
            PossibleExtraFiles = possibleExtraFiles;
        }
    }
}
