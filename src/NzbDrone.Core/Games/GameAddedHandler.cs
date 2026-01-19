using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.Games
{
    public class GameAddedHandler : IHandle<GameAddedEvent>, IHandle<GamesImportedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public GameAddedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(GameAddedEvent message)
        {
            _commandQueueManager.Push(new RefreshGameCommand(new List<int> { message.Game.Id }, true));
        }

        public void Handle(GamesImportedEvent message)
        {
            _commandQueueManager.PushMany(message.Games.Select(s => new RefreshGameCommand(new List<int> { s.Id }, true)).ToList());
        }
    }
}
