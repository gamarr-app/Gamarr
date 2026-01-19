using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Games.Events
{
    public class GameEditedEvent : IEvent
    {
        public Game Game { get; private set; }
        public Game OldGame { get; private set; }

        public GameEditedEvent(Game game, Game oldGame)
        {
            Game = game;
            OldGame = oldGame;
        }
    }
}
