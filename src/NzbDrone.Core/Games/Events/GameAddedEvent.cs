using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Games.Events
{
    public class GameAddedEvent : IEvent
    {
        public Game Game { get; private set; }

        public GameAddedEvent(Game game)
        {
            Game = game;
        }
    }
}
