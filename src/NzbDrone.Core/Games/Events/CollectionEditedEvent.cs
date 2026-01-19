using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games.Collections;

namespace NzbDrone.Core.Games.Events
{
    public class CollectionEditedEvent : IEvent
    {
        public GameCollection Collection { get; private set; }
        public GameCollection OldCollection { get; private set; }

        public CollectionEditedEvent(GameCollection collection, GameCollection oldCollection)
        {
            Collection = collection;
            OldCollection = oldCollection;
        }
    }
}
