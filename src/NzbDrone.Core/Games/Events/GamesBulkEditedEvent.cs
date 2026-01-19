using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Games.Events
{
    public class GamesBulkEditedEvent : IEvent
    {
        public IReadOnlyCollection<Game> Games { get; private set; }

        public GamesBulkEditedEvent(IReadOnlyCollection<Game> games)
        {
            Games = games;
        }
    }
}
