using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games.Collections;

namespace NzbDrone.Core.Games.Events
{
    public class CollectionAddedEvent : IEvent
    {
        public GameCollection Collection { get; private set; }

        public CollectionAddedEvent(GameCollection collection)
        {
            Collection = collection;
        }
    }
}
