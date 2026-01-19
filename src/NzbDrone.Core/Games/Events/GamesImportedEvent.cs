using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Games.Events
{
    public class GamesImportedEvent : IEvent
    {
        public List<Game> Games { get; private set; }

        public GamesImportedEvent(List<Game> games)
        {
            Games = games;
        }
    }
}
