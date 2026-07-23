using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.SignalR;
using Gamarr.Http;
using Gamarr.Http.REST;
using Gamarr.Http.REST.Attributes;
using BadRequestException = Gamarr.Http.REST.BadRequestException;

namespace Gamarr.Api.V3.GameFiles
{
    [V3ApiController]
    public class GameFileController : RestControllerWithSignalR<GameFileResource, GameFile>,
                                 IHandle<GameFileAddedEvent>,
                                 IHandle<GameFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IGameService _gameService;
        private readonly IGameComponentService _gameComponentService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public GameFileController(IBroadcastSignalRMessage signalRBroadcaster,
                                IMediaFileService mediaFileService,
                                IDeleteMediaFiles mediaFileDeletionService,
                                IGameService gameService,
                                IGameComponentService gameComponentService,
                                ICustomFormatCalculationService formatCalculator,
                                IUpgradableSpecification upgradableSpecification)
            : base(signalRBroadcaster)
        {
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _gameService = gameService;
            _gameComponentService = gameComponentService;
            _formatCalculator = formatCalculator;
            _upgradableSpecification = upgradableSpecification;
        }

        protected override GameFileResource GetResourceById(int id)
        {
            var gameFile = _mediaFileService.GetGame(id);
            var game = _gameService.GetGame(gameFile.GameId);
            var componentsById = _gameComponentService.GetByGame(game.Id).ToDictionary(x => x.Id);
            componentsById.TryGetValue(gameFile.ComponentId, out var component);

            var resource = gameFile.ToResource(game, _upgradableSpecification, _formatCalculator, component);

            return resource;
        }

        [HttpGet]
        [Produces("application/json")]
        public List<GameFileResource> GetGameFiles([FromQuery(Name = "gameId")] List<int> gameIds, [FromQuery] List<int> gameFileIds)
        {
            if (!gameIds.Any() && !gameFileIds.Any())
            {
                throw new BadRequestException("gameId or gameFileIds must be provided");
            }

            var gameFiles = gameIds.Any()
                ? _mediaFileService.GetFilesByGames(gameIds)
                : _mediaFileService.GetGames(gameFileIds);

            if (gameFiles == null)
            {
                return new List<GameFileResource>();
            }

            return gameFiles.GroupBy(e => e.GameId)
                .SelectMany(group =>
                {
                    var game = _gameService.GetGame(group.Key);
                    var componentsById = _gameComponentService.GetByGame(group.Key).ToDictionary(x => x.Id);

                    return group.Select(file =>
                    {
                        componentsById.TryGetValue(file.ComponentId, out var component);
                        return file.ToResource(game, _upgradableSpecification, _formatCalculator, component);
                    });
                })
                .ToList();
        }

        [RestPutById]
        [Consumes("application/json")]
        public ActionResult<GameFileResource> SetGameFile([FromBody] GameFileResource gameFileResource)
        {
            var gameFile = _mediaFileService.GetGame(gameFileResource.Id);

            // Omitted fields must keep their current values (the bulk endpoint
            // below already does this); casting a null IndexerFlags was a 500.
            if (gameFileResource.IndexerFlags.HasValue)
            {
                gameFile.IndexerFlags = (IndexerFlags)gameFileResource.IndexerFlags;
            }

            if (gameFileResource.Quality != null)
            {
                gameFile.Quality = gameFileResource.Quality;
            }

            if (gameFileResource.Languages != null)
            {
                gameFile.Languages = gameFileResource.Languages;
            }

            gameFile.Edition = gameFileResource.Edition;
            if (gameFileResource.ReleaseGroup != null)
            {
                gameFile.ReleaseGroup = gameFileResource.ReleaseGroup;
            }

            if (gameFileResource.SceneName != null && SceneChecker.IsSceneTitle(gameFileResource.SceneName))
            {
                gameFile.SceneName = gameFileResource.SceneName;
            }

            _mediaFileService.Update(gameFile);
            return Accepted(gameFile.Id);
        }

        [RestDeleteById]
        public void DeleteGameFile(int id)
        {
            var gameFile = _mediaFileService.GetGame(id);

            if (gameFile == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Game file not found");
            }

            var game = _gameService.GetGame(gameFile.GameId);

            _mediaFileDeletionService.DeleteGameFile(game, gameFile);
        }

        [HttpDelete("bulk")]
        [Consumes("application/json")]
        public object DeleteGameFiles([FromBody] GameFileListResource resource)
        {
            if (!resource.GameFileIds.Any())
            {
                throw new BadRequestException("gameFileIds must be provided");
            }

            var gameFiles = _mediaFileService.GetGames(resource.GameFileIds);
            var game = _gameService.GetGame(gameFiles.First().GameId);

            foreach (var gameFile in gameFiles)
            {
                _mediaFileDeletionService.DeleteGameFile(game, gameFile);
            }

            return new { };
        }

        [HttpPut("bulk")]
        [Consumes("application/json")]
        public object SetPropertiesBulk([FromBody] List<GameFileResource> resources)
        {
            var gameFiles = _mediaFileService.GetGames(resources.Select(r => r.Id));

            foreach (var gameFile in gameFiles)
            {
                var resourceGameFile = resources.Single(r => r.Id == gameFile.Id);

                if (resourceGameFile.Languages != null)
                {
                    // Don't allow user to set files with 'Any' or 'Original' language
                    gameFile.Languages = resourceGameFile.Languages.Where(l => l != null && l != Language.Any && l != Language.Original).ToList();
                }

                if (resourceGameFile.Quality != null)
                {
                    gameFile.Quality = resourceGameFile.Quality;
                }

                if (resourceGameFile.SceneName != null && SceneChecker.IsSceneTitle(resourceGameFile.SceneName))
                {
                    gameFile.SceneName = resourceGameFile.SceneName;
                }

                if (resourceGameFile.Edition != null)
                {
                    gameFile.Edition = resourceGameFile.Edition;
                }

                if (resourceGameFile.ReleaseGroup != null)
                {
                    gameFile.ReleaseGroup = resourceGameFile.ReleaseGroup;
                }

                if (resourceGameFile.Version != null)
                {
                    gameFile.GameVersion = GameVersion.Parse(resourceGameFile.Version);
                }

                if (resourceGameFile.IndexerFlags.HasValue)
                {
                    gameFile.IndexerFlags = (IndexerFlags)resourceGameFile.IndexerFlags;
                }
            }

            _mediaFileService.Update(gameFiles);

            var game = _gameService.GetGame(gameFiles.First().GameId);

            var componentsById = _gameComponentService.GetByGame(game.Id).ToDictionary(x => x.Id);

            return Accepted(gameFiles.ConvertAll(f =>
            {
                componentsById.TryGetValue(f.ComponentId, out var component);
                return f.ToResource(game, _upgradableSpecification, _formatCalculator, component);
            }));
        }

        [NonAction]
        public void Handle(GameFileAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.GameFile.Id);
        }

        [NonAction]
        public void Handle(GameFileDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.GameFile.Id);
        }
    }
}
