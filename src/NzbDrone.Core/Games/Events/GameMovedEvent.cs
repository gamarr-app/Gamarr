using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Games.Events
{
    public class GameMovedEvent : IEvent
    {
        public Game Game { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }

        public GameMovedEvent(Game game, string sourcePath, string destinationPath)
        {
            Game = game;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }
    }
}
