using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.Games
{
    public class RefreshCollectionService : IExecute<RefreshCollectionsCommand>
    {
        private readonly IProvideGameInfo _gameInfo;
        private readonly IGameCollectionService _collectionService;
        private readonly IGameService _gameService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly IAddGameService _addGameService;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly IEventAggregator _eventAggregator;

        private readonly Logger _logger;

        public RefreshCollectionService(IProvideGameInfo gameInfo,
                                        IGameCollectionService collectionService,
                                        IGameService gameService,
                                        IGameMetadataService gameMetadataService,
                                        IAddGameService addGameService,
                                        IImportListExclusionService importListExclusionService,
                                        IEventAggregator eventAggregator,
                                        Logger logger)
        {
            _gameInfo = gameInfo;
            _collectionService = collectionService;
            _gameService = gameService;
            _gameMetadataService = gameMetadataService;
            _addGameService = addGameService;
            _importListExclusionService = importListExclusionService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        private GameCollection RefreshCollectionInfo(int collectionId)
        {
            // Get the game before updating, that way any changes made to the game after the refresh started,
            // but before this game was refreshed won't be lost.
            var collection = _collectionService.GetCollection(collectionId);

            _logger.ProgressInfo("Updating info for {0}", collection.Title);

            GameCollection collectionInfo;
            List<GameMetadata> games;

            try
            {
                collectionInfo = _gameInfo.GetCollectionInfo(collection.IgdbId);
            }
            catch (GameNotFoundException)
            {
                _collectionService.RemoveCollection(collection);
                _logger.Debug("Removing collection not present on TMDb for {0}", collection.Title);

                throw;
            }

            collection.Title = collectionInfo.Title;
            collection.Overview = collectionInfo.Overview;
            collection.CleanTitle = collectionInfo.CleanTitle;
            collection.SortTitle = collectionInfo.SortTitle;
            collection.LastInfoSync = DateTime.UtcNow;
            collection.Images = collectionInfo.Images;

            games = collectionInfo.Games;
            games.ForEach(x => x.CollectionIgdbId = collection.IgdbId);

            var existingMetaForCollection = _gameMetadataService.GetGamesByCollectionIgdbId(collection.IgdbId);

            var updateList = new List<GameMetadata>();

            foreach (var remoteGame in games)
            {
                var existing = existingMetaForCollection.FirstOrDefault(e => e.IgdbId == remoteGame.IgdbId);

                if (existingMetaForCollection.Any(x => x.IgdbId == remoteGame.IgdbId))
                {
                    existingMetaForCollection.Remove(existing);
                }

                updateList.Add(remoteGame);
            }

            _gameMetadataService.UpsertMany(updateList);
            _gameMetadataService.DeleteMany(existingMetaForCollection);

            _logger.Debug("Finished collection refresh for {0}", collection.Title);

            _collectionService.UpdateCollection(collection);

            return collection;
        }

        public bool ShouldRefresh(GameCollection collection)
        {
            if (collection.LastInfoSync == null || collection.LastInfoSync < DateTime.UtcNow.AddDays(-15))
            {
                _logger.Trace("Collection {0} last updated more than 15 days ago, should refresh.", collection.Title);
                return true;
            }

            if (collection.LastInfoSync >= DateTime.UtcNow.AddHours(-6))
            {
                _logger.Trace("Collection {0} last updated less than 6 hours ago, should not be refreshed.", collection.Title);
                return false;
            }

            return false;
        }

        private void SyncCollectionGames(GameCollection collection)
        {
            if (collection.Monitored)
            {
                var collectionGames = _gameMetadataService
                    .GetGamesByCollectionIgdbId(collection.IgdbId)
                    .Where(m => m.Status is GameStatusType.EarlyAccess or GameStatusType.Released)
                    .ToList();

                var existingGames = _gameService.AllGameIgdbIds();
                var excludedGames = _importListExclusionService.All().Select(e => e.IgdbId);
                var gamesToAdd = collectionGames.Where(m => !existingGames.Contains(m.IgdbId)).Where(m => !excludedGames.Contains(m.IgdbId)).ToList();

                if (gamesToAdd.Any())
                {
                    _addGameService.AddGames(gamesToAdd.Select(m => new Game
                    {
                        IgdbId = m.IgdbId,
                        Title = m.Title,
                        QualityProfileId = collection.QualityProfileId,
                        RootFolderPath = collection.RootFolderPath,
                        MinimumAvailability = collection.MinimumAvailability,
                        AddOptions = new AddGameOptions
                        {
                            SearchForGame = collection.SearchOnAdd,
                            AddMethod = AddGameMethod.Collection
                        },
                        Monitored = true,
                        Tags = collection.Tags
                    }).ToList(), true);
                }
            }
        }

        public void Execute(RefreshCollectionsCommand message)
        {
            if (message.CollectionIds.Any())
            {
                foreach (var collectionId in message.CollectionIds)
                {
                    var newCollection = RefreshCollectionInfo(collectionId);
                    SyncCollectionGames(newCollection);
                }
            }
            else
            {
                var allCollections = _collectionService.GetAllCollections().OrderBy(c => c.SortTitle).ToList();

                foreach (var collection in allCollections)
                {
                    try
                    {
                        var newCollection = collection;

                        if (ShouldRefresh(collection) || message.Trigger == CommandTrigger.Manual)
                        {
                            newCollection = RefreshCollectionInfo(collection.Id);
                        }

                        SyncCollectionGames(newCollection);
                    }
                    catch (GameNotFoundException)
                    {
                        _logger.Error("Collection '{0}' (TMDb {1}) was not found, it may have been removed from The Game Database.", collection.Title, collection.IgdbId);
                    }
                }
            }

            _eventAggregator.PublishEvent(new CollectionRefreshCompleteEvent());
        }
    }
}
