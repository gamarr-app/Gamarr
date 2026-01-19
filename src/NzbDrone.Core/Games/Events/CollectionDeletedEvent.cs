using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games.Collections;

namespace NzbDrone.Core.Games.Events
{
    public class CollectionDeletedEvent : IEvent
    {
        public GameCollection Collection { get; private set; }

        public CollectionDeletedEvent(GameCollection collection)
        {
            Collection = collection;
        }
    }
}
