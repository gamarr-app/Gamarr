using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameRenamedEvent : IEvent
    {
        public Game Game { get; private set; }
        public List<RenamedGameFile> RenamedFiles { get; private set; }

        public GameRenamedEvent(Game game, List<RenamedGameFile> renamedFiles)
        {
            Game = game;
            RenamedFiles = renamedFiles;
        }
    }
}
