using System.Collections.Generic;
using NLog;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Collections;

namespace NzbDrone.Core.Games
{
    public class GameScannedHandler : IHandle<GameScannedEvent>,
                                        IHandle<GameScanSkippedEvent>
    {
        private readonly IGameService _gameService;
        private readonly IGameCollectionService _collectionService;
        private readonly IManageCommandQueue _commandQueueManager;

        private readonly Logger _logger;

        public GameScannedHandler(IGameService gameService,
                                    IGameCollectionService collectionService,
                                    IManageCommandQueue commandQueueManager,
                                    Logger logger)
        {
            _gameService = gameService;
            _collectionService = collectionService;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        private void HandleScanEvents(Game game)
        {
            var addOptions = game.AddOptions;

            if (addOptions == null)
            {
                return;
            }

            _logger.Info("[{0}] was recently added, performing post-add actions", game.Title);

            if (addOptions.SearchForGame)
            {
                _commandQueueManager.Push(new GamesSearchCommand { GameIds = new List<int> { game.Id } });
            }

            if (addOptions.Monitor == MonitorTypes.GameAndCollection && game.GameMetadata.Value.CollectionIgdbId > 0)
            {
                var collection = _collectionService.FindByIgdbId(game.GameMetadata.Value.CollectionIgdbId);
                collection.Monitored = true;

                _collectionService.UpdateCollection(collection);
            }

            game.AddOptions = null;
            _gameService.RemoveAddOptions(game);
        }

        public void Handle(GameScannedEvent message)
        {
            HandleScanEvents(message.Game);
        }

        public void Handle(GameScanSkippedEvent message)
        {
            HandleScanEvents(message.Game);
        }
    }
}
