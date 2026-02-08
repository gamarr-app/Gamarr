using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.GameStats;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.SignalR;
using Gamarr.Http;
using Gamarr.Http.REST;
using Gamarr.Http.REST.Attributes;

namespace Gamarr.Api.V3.Games
{
    [V3ApiController]
    public class GameController : RestControllerWithSignalR<GameResource, Game>,
                                IHandle<GameUpdatedEvent>,
                                IHandle<GameEditedEvent>,
                                IHandle<GamesDeletedEvent>,
                                IHandle<GameRenamedEvent>,
                                IHandle<GamesBulkEditedEvent>,
                                IHandle<MediaCoversUpdatedEvent>
    {
        private readonly IGameService _gamesService;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly IAddGameService _addGameService;
        private readonly IGameStatisticsService _gameStatisticsService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IRootFolderService _rootFolderService;
        private readonly IUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IConfigService _configService;

        public GameController(IBroadcastSignalRMessage signalRBroadcaster,
                           IGameService gamesService,
                           IGameTranslationService gameTranslationService,
                           IAddGameService addGameService,
                           IGameStatisticsService gameStatisticsService,
                           IMapCoversToLocal coverMapper,
                           IManageCommandQueue commandQueueManager,
                           IRootFolderService rootFolderService,
                           IUpgradableSpecification qualityUpgradableSpecification,
                           IConfigService configService,
                           RootFolderValidator rootFolderValidator,
                           MappedNetworkDriveValidator mappedNetworkDriveValidator,
                           GamePathValidator gamesPathValidator,
                           GameExistsValidator gamesExistsValidator,
                           GameAncestorValidator gamesAncestorValidator,
                           RecycleBinValidator recycleBinValidator,
                           SystemFolderValidator systemFolderValidator,
                           QualityProfileExistsValidator qualityProfileExistsValidator,
                           RootFolderExistsValidator rootFolderExistsValidator,
                           GameFolderAsRootFolderValidator gameFolderAsRootFolderValidator)
            : base(signalRBroadcaster)
        {
            _gamesService = gamesService;
            _gameTranslationService = gameTranslationService;
            _addGameService = addGameService;
            _gameStatisticsService = gameStatisticsService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _configService = configService;
            _coverMapper = coverMapper;
            _commandQueueManager = commandQueueManager;
            _rootFolderService = rootFolderService;

            SharedValidator.RuleFor(s => s.Path).Cascade(CascadeMode.Stop)
                .IsValidPath()
                .SetPathValidator(rootFolderValidator)
                .SetPathValidator(mappedNetworkDriveValidator)
                .Must((resource, path) => gamesPathValidator.Validate(path, resource.Id))
                .WithMessage("Path is already used by another game")
                .SetPathValidator(gamesAncestorValidator)
                .SetPathValidator(recycleBinValidator)
                .SetPathValidator(systemFolderValidator)
                .When(s => s.Path.IsNotNullOrWhiteSpace());

            PostValidator.RuleFor(s => s.Path).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .IsValidPath()
                .When(s => s.RootFolderPath.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.RootFolderPath).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .IsValidPath()
                .SetPathValidator(rootFolderExistsValidator)
                .Must((resource, path) => gameFolderAsRootFolderValidator.IsValid(new ValidationContext<object>(resource), path))
                .WithMessage("Root folder path contains game folder")
                .When(s => s.Path.IsNullOrWhiteSpace());

            PutValidator.RuleFor(s => s.Path).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .IsValidPath();

            SharedValidator.RuleFor(s => s.QualityProfileId).Cascade(CascadeMode.Stop)
                .ValidId()
                .SetValidator(qualityProfileExistsValidator);

            PostValidator.RuleFor(s => s.Title).NotEmpty().When(s => s.IgdbId <= 0 && s.SteamAppId <= 0);

            // Allow either IgdbId or SteamAppId (for Steam-only games)
            PostValidator.RuleFor(s => s.IgdbId)
                .NotNull()
                .NotEmpty()
                .SetPathValidator(gamesExistsValidator)
                .When(s => s.SteamAppId <= 0);
            PostValidator.RuleFor(s => s.SteamAppId)
                .GreaterThan(0)
                .SetSteamAppIdValidator(gamesExistsValidator)
                .When(s => s.IgdbId <= 0)
                .WithMessage("Either IgdbId or SteamAppId must be provided");
        }

        [HttpGet]
        public List<GameResource> AllGame(int? igdbId, bool excludeLocalCovers = false, int? languageId = null)
        {
            var gamesResources = new List<GameResource>();

            var translationLanguage = languageId is > 0
                ? Language.All.Single(l => l.Id == languageId.Value)
                : (Language)_configService.GameInfoLanguage;

            if (igdbId.HasValue)
            {
                var game = _gamesService.FindByIgdbId(igdbId.Value);

                if (game != null)
                {
                    gamesResources.AddIfNotNull(MapToResource(game, translationLanguage));
                }
            }
            else
            {
                var gameStats = _gameStatisticsService.GameStatistics();
                var availDelay = _configService.AvailabilityDelay;

                var gameTask = Task.Run(() => _gamesService.GetAllGames());

                var translations = _gameTranslationService
                    .GetAllTranslationsForLanguage(translationLanguage);

                var tdict = translations.ToDictionaryIgnoreDuplicates(x => x.GameMetadataId);
                var sdict = gameStats.ToDictionary(x => x.GameId);

                var games = gameTask.GetAwaiter().GetResult();

                gamesResources = new List<GameResource>(games.Count);

                foreach (var game in games)
                {
                    var translation = GetTranslationFromDict(tdict, game.GameMetadata, translationLanguage);
                    gamesResources.Add(game.ToResource(availDelay, translation, _qualityUpgradableSpecification));
                }

                if (!excludeLocalCovers)
                {
                    var coverFileInfos = _coverMapper.GetCoverFileInfos();

                    MapCoversToLocal(gamesResources, coverFileInfos);
                }

                LinkGameStatistics(gamesResources, sdict);

                var rootFolders = _rootFolderService.All();

                gamesResources.ForEach(m => m.RootFolderPath = _rootFolderService.GetBestRootFolderPath(m.Path, rootFolders));
            }

            return gamesResources;
        }

        protected override GameResource GetResourceById(int id)
        {
            var game = _gamesService.GetGame(id);

            return MapToResource(game);
        }

        protected GameResource MapToResource(Game game, Language translationLanguage = null)
        {
            if (game == null)
            {
                return null;
            }

            translationLanguage ??= (Language)_configService.GameInfoLanguage;
            var availDelay = _configService.AvailabilityDelay;

            var translations = _gameTranslationService.GetAllTranslationsForGameMetadata(game.GameMetadataId);
            var translation = GetGameTranslation(translations, game.GameMetadata, translationLanguage);

            var resource = game.ToResource(availDelay, translation, _qualityUpgradableSpecification);
            MapCoversToLocal(resource);
            FetchAndLinkGameStatistics(resource);

            resource.RootFolderPath = _rootFolderService.GetBestRootFolderPath(resource.Path);

            return resource;
        }

        private GameTranslation GetGameTranslation(List<GameTranslation> translations, GameMetadata game, Language configLanguage)
        {
            if (configLanguage == Language.Original)
            {
                return new GameTranslation
                {
                    Title = game.OriginalTitle,
                    Overview = game.Overview
                };
            }

            return translations.FirstOrDefault(t => t.Language == configLanguage && t.GameMetadataId == game.Id);
        }

        private GameTranslation GetTranslationFromDict(Dictionary<int, GameTranslation> translations, GameMetadata game, Language configLanguage)
        {
            if (configLanguage == Language.Original)
            {
                return new GameTranslation
                {
                    Title = game.OriginalTitle,
                    Overview = game.Overview
                };
            }

            if (!translations.TryGetValue(game.Id, out var translation))
            {
                translation = new GameTranslation
                {
                    Title = game.Title,
                    Language = Language.English
                };
            }

            return translation;
        }

        [RestPostById]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<GameResource> AddGame([FromBody] GameResource gamesResource)
        {
            var game = _addGameService.AddGame(gamesResource.ToModel());

            return Created(game.Id);
        }

        [RestPutById]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<GameResource> UpdateGame([FromBody] GameResource gamesResource, [FromQuery] bool moveFiles = false)
        {
            var game = _gamesService.GetGame(gamesResource.Id);

            if (moveFiles)
            {
                var sourcePath = game.Path;
                var destinationPath = gamesResource.Path;

                _commandQueueManager.Push(new MoveGameCommand
                {
                    GameId = game.Id,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath
                }, trigger: CommandTrigger.Manual);
            }

            var model = gamesResource.ToModel(game);

            var updatedGame = _gamesService.UpdateGame(model);

            BroadcastResourceChange(ModelAction.Updated, MapToResource(updatedGame));

            return Accepted(gamesResource.Id);
        }

        [RestDeleteById]
        public void DeleteGame(int id, bool deleteFiles = false, bool addImportExclusion = false)
        {
            _gamesService.DeleteGame(id, deleteFiles, addImportExclusion);
        }

        /// <summary>
        /// Get all DLCs/expansions for a specific game
        /// </summary>
        [HttpGet("{id}/dlc")]
        public List<GameResource> GetDlcsForGame(int id)
        {
            var game = _gamesService.GetGame(id);

            if (game == null || game.IgdbId <= 0)
            {
                return new List<GameResource>();
            }

            var availDelay = _configService.AvailabilityDelay;
            var dlcs = _gamesService.GetDlcsForGame(game.IgdbId);

            return dlcs.Select(d => d.ToResource(availDelay, null, _qualityUpgradableSpecification)).ToList();
        }

        /// <summary>
        /// Get the parent game for a DLC
        /// </summary>
        [HttpGet("{id}/parent")]
        public ActionResult<GameResource> GetParentGame(int id)
        {
            var game = _gamesService.GetGame(id);

            if (game == null || !game.GameMetadata.Value.ParentGameId.HasValue)
            {
                return NotFound();
            }

            var parent = _gamesService.GetParentGame(game.GameMetadata.Value.ParentGameId.Value);

            if (parent == null)
            {
                return NotFound();
            }

            return MapToResource(parent);
        }

        /// <summary>
        /// Get all DLCs in the library
        /// </summary>
        [HttpGet("dlc")]
        public List<GameResource> GetAllDlcs()
        {
            var availDelay = _configService.AvailabilityDelay;
            var dlcs = _gamesService.GetAllDlcs();

            var resources = dlcs.Select(d => d.ToResource(availDelay, null, _qualityUpgradableSpecification)).ToList();
            MapCoversToLocal(resources, _coverMapper.GetCoverFileInfos());

            return resources;
        }

        /// <summary>
        /// Get only main games (excluding DLCs)
        /// </summary>
        [HttpGet("main")]
        public List<GameResource> GetMainGamesOnly()
        {
            var availDelay = _configService.AvailabilityDelay;
            var games = _gamesService.GetMainGamesOnly();

            var resources = games.Select(g => g.ToResource(availDelay, null, _qualityUpgradableSpecification)).ToList();
            MapCoversToLocal(resources, _coverMapper.GetCoverFileInfos());

            return resources;
        }

        private void MapCoversToLocal(GameResource game)
        {
            _coverMapper.ConvertToLocalUrls(game.Id, game.Images);
        }

        private void MapCoversToLocal(IEnumerable<GameResource> games, Dictionary<string, FileInfo> coverFileInfos)
        {
            _coverMapper.ConvertToLocalUrls(games.Select(x => Tuple.Create(x.Id, x.Images.AsEnumerable())), coverFileInfos);
        }

        private void FetchAndLinkGameStatistics(GameResource resource)
        {
            LinkGameStatistics(resource, _gameStatisticsService.GameStatistics(resource.Id));
        }

        private void LinkGameStatistics(List<GameResource> resources, Dictionary<int, GameStatistics> sDict)
        {
            foreach (var game in resources)
            {
                if (sDict.TryGetValue(game.Id, out var stats))
                {
                    LinkGameStatistics(game, stats);
                }
            }
        }

        private void LinkGameStatistics(GameResource resource, GameStatistics gameStatistics)
        {
            resource.Statistics = gameStatistics.ToResource();
            resource.HasFile = gameStatistics.GameFileCount > 0;
            resource.SizeOnDisk = gameStatistics.SizeOnDisk;
        }

        [NonAction]
        public void Handle(GameUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Game));
        }

        [NonAction]
        public void Handle(GameEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Game));
        }

        [NonAction]
        public void Handle(GamesDeletedEvent message)
        {
            foreach (var game in message.Games)
            {
                BroadcastResourceChange(ModelAction.Deleted, game.Id);
            }
        }

        [NonAction]
        public void Handle(GameRenamedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Game));
        }

        [NonAction]
        public void Handle(GamesBulkEditedEvent message)
        {
            foreach (var game in message.Games)
            {
                BroadcastResourceChange(ModelAction.Updated, MapToResource(game));
            }
        }

        [NonAction]
        public void Handle(MediaCoversUpdatedEvent message)
        {
            if (message.Updated)
            {
                BroadcastResourceChange(ModelAction.Updated, message.Game.Id);
            }
        }
    }
}
