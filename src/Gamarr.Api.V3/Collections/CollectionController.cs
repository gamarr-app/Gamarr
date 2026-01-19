using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Organizer;
using NzbDrone.SignalR;
using Gamarr.Http;
using Gamarr.Http.REST;
using Gamarr.Http.REST.Attributes;

namespace Gamarr.Api.V3.Collections
{
    [V3ApiController]
    public class CollectionController : RestControllerWithSignalR<CollectionResource, GameCollection>,
                                        IHandle<CollectionAddedEvent>,
                                        IHandle<CollectionEditedEvent>,
                                        IHandle<CollectionDeletedEvent>
    {
        private readonly IGameCollectionService _collectionService;
        private readonly IGameService _gameService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly IConfigService _configService;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly INamingConfigService _namingService;
        private readonly IManageCommandQueue _commandQueueManager;

        public CollectionController(IBroadcastSignalRMessage signalRBroadcaster,
                                    IGameCollectionService collectionService,
                                    IGameService gameService,
                                    IGameMetadataService gameMetadataService,
                                    IGameTranslationService gameTranslationService,
                                    IImportListExclusionService importListExclusionService,
                                    IConfigService configService,
                                    IBuildFileNames fileNameBuilder,
                                    INamingConfigService namingService,
                                    IManageCommandQueue commandQueueManager)
            : base(signalRBroadcaster)
        {
            _collectionService = collectionService;
            _gameService = gameService;
            _gameMetadataService = gameMetadataService;
            _gameTranslationService = gameTranslationService;
            _importListExclusionService = importListExclusionService;
            _configService = configService;
            _fileNameBuilder = fileNameBuilder;
            _namingService = namingService;
            _commandQueueManager = commandQueueManager;
        }

        protected override CollectionResource GetResourceById(int id)
        {
            return MapToResource(_collectionService.GetCollection(id));
        }

        [HttpGet]
        [Produces("application/json")]
        public List<CollectionResource> GetCollections(int? igdbId)
        {
            var collectionResources = new List<CollectionResource>();

            if (igdbId.HasValue)
            {
                var collection = _collectionService.FindByIgdbId(igdbId.Value);

                if (collection != null)
                {
                    collectionResources.AddIfNotNull(MapToResource(collection));
                }
            }
            else
            {
                collectionResources = MapToResource(_collectionService.GetAllCollections()).ToList();
            }

            return collectionResources;
        }

        [RestPutById]
        [Consumes("application/json")]
        public ActionResult<CollectionResource> UpdateCollection([FromBody] CollectionResource collectionResource)
        {
            var collection = _collectionService.GetCollection(collectionResource.Id);

            var model = collectionResource.ToModel(collection);

            var updatedGame = _collectionService.UpdateCollection(model);

            return Accepted(updatedGame.Id);
        }

        [HttpPut]
        [Consumes("application/json")]
        public ActionResult UpdateCollections([FromBody] CollectionUpdateResource resource)
        {
            var collectionsToUpdate = _collectionService.GetCollections(resource.CollectionIds).ToList();

            foreach (var collection in collectionsToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    collection.Monitored = resource.Monitored.Value;
                }

                if (resource.QualityProfileId.HasValue)
                {
                    collection.QualityProfileId = resource.QualityProfileId.Value;
                }

                if (resource.MinimumAvailability.HasValue)
                {
                    collection.MinimumAvailability = resource.MinimumAvailability.Value;
                }

                if (resource.RootFolderPath.IsNotNullOrWhiteSpace())
                {
                    collection.RootFolderPath = resource.RootFolderPath;
                }

                if (resource.SearchOnAdd.HasValue)
                {
                    collection.SearchOnAdd = resource.SearchOnAdd.Value;
                }

                if (resource.MonitorGames.HasValue)
                {
                    var games = _gameService.GetGamesByCollectionIgdbId(collection.IgdbId);

                    games.ForEach(c => c.Monitored = resource.MonitorGames.Value);

                    _gameService.UpdateGame(games, true);
                }
            }

            var updated = _collectionService.UpdateCollections(collectionsToUpdate).ToResource();

            _commandQueueManager.Push(new RefreshCollectionsCommand());

            return Accepted(updated);
        }

        private IEnumerable<CollectionResource> MapToResource(List<GameCollection> collections)
        {
            // Avoid calling for naming spec on every game in filenamebuilder
            var namingConfig = _namingService.GetConfig();
            var configLanguage = (Language)_configService.GameInfoLanguage;

            var existingGamesIgdbIds = _gameService.AllGameWithCollectionsIgdbIds();
            var listExclusions = _importListExclusionService.All();

            var allCollectionGames = _gameMetadataService.GetGamesWithCollections()
                .GroupBy(x => x.CollectionIgdbId)
                .ToDictionary(x => x.Key, x => (IEnumerable<GameMetadata>)x);

            var translations = _gameTranslationService.GetAllTranslationsForLanguage(configLanguage);
            var tdict = translations.ToDictionaryIgnoreDuplicates(x => x.GameMetadataId);

            foreach (var collection in collections)
            {
                var resource = collection.ToResource();

                allCollectionGames.TryGetValue(collection.IgdbId, out var collectionGames);

                if (collectionGames != null)
                {
                    foreach (var game in collectionGames)
                    {
                        var translation = GetTranslationFromDict(tdict, game, configLanguage);

                        var gameResource = game.ToResource(translation);
                        gameResource.Folder = _fileNameBuilder.GetGameFolder(new Game { GameMetadata = game }, namingConfig);

                        var isExisting = existingGamesIgdbIds.Contains(game.IgdbId);
                        gameResource.IsExisting = isExisting;

                        var isExcluded = listExclusions.Any(e => e.IgdbId == game.IgdbId);
                        gameResource.IsExcluded = isExcluded;

                        if (!isExisting && !isExcluded)
                        {
                            resource.MissingGames++;
                        }

                        resource.Games.Add(gameResource);
                    }
                }

                yield return resource;
            }
        }

        private CollectionResource MapToResource(GameCollection collection)
        {
            var resource = collection.ToResource();

            var namingConfig = _namingService.GetConfig();
            var configLanguage = (Language)_configService.GameInfoLanguage;

            var existingGamesIgdbIds = _gameService.AllGameWithCollectionsIgdbIds();
            var listExclusions = _importListExclusionService.All();

            foreach (var game in _gameMetadataService.GetGamesByCollectionIgdbId(collection.IgdbId))
            {
                var translations = _gameTranslationService.GetAllTranslationsForGameMetadata(game.Id);
                var translation = GetGameTranslation(translations, game, configLanguage);

                var gameResource = game.ToResource(translation);
                gameResource.Folder = _fileNameBuilder.GetGameFolder(new Game { GameMetadata = game }, namingConfig);

                var isExisting = existingGamesIgdbIds.Contains(game.IgdbId);
                gameResource.IsExisting = isExisting;

                var isExcluded = listExclusions.Any(e => e.IgdbId == game.IgdbId);
                gameResource.IsExcluded = isExcluded;

                if (!isExisting && !isExcluded)
                {
                    resource.MissingGames++;
                }

                resource.Games.Add(gameResource);
            }

            return resource;
        }

        private GameTranslation GetGameTranslation(List<GameTranslation> translations, GameMetadata gameMetadata, Language configLanguage)
        {
            if (configLanguage == Language.Original)
            {
                return new GameTranslation
                {
                    Title = gameMetadata.OriginalTitle,
                    Overview = gameMetadata.Overview
                };
            }

            var translation = translations.FirstOrDefault(t => t.Language == configLanguage && t.GameMetadataId == gameMetadata.Id);

            if (translation == null)
            {
                translation = new GameTranslation
                {
                    Title = gameMetadata.Title,
                    Language = Language.English
                };
            }

            return translation;
        }

        private GameTranslation GetTranslationFromDict(Dictionary<int, GameTranslation> translations, GameMetadata gameMetadata, Language configLanguage)
        {
            if (configLanguage == Language.Original)
            {
                return new GameTranslation
                {
                    Title = gameMetadata.OriginalTitle,
                    Overview = gameMetadata.Overview
                };
            }

            if (!translations.TryGetValue(gameMetadata.Id, out var translation))
            {
                translation = new GameTranslation
                {
                    Title = gameMetadata.Title,
                    Language = Language.English
                };
            }

            return translation;
        }

        [NonAction]
        public void Handle(CollectionAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Created, MapToResource(message.Collection));
        }

        [NonAction]
        public void Handle(CollectionEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Collection));
        }

        [NonAction]
        public void Handle(CollectionDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Collection.Id);
        }
    }
}
