using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.Games
{
    public class GameCollectionAddedHandler : IHandle<CollectionAddedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public GameCollectionAddedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(CollectionAddedEvent message)
        {
            _commandQueueManager.Push(new RefreshCollectionsCommand(new List<int> { message.Collection.Id }));
        }
    }
}
