using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.GameStats;
using NzbDrone.SignalR;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Games
{
    public abstract class GameControllerWithSignalR : RestControllerWithSignalR<GameResource, Game>,
                                                         IHandle<GameGrabbedEvent>,
                                                         IHandle<GameFileImportedEvent>,
                                                         IHandle<GameFileDeletedEvent>
    {
        protected readonly IGameService _gameService;
        protected readonly IGameTranslationService _gameTranslationService;
        protected readonly IGameStatisticsService _gameStatisticsService;
        protected readonly IUpgradableSpecification _upgradableSpecification;
        protected readonly ICustomFormatCalculationService _formatCalculator;
        protected readonly IConfigService _configService;
        protected readonly IMapCoversToLocal _coverMapper;

        protected GameControllerWithSignalR(IGameService gameService,
                                           IGameTranslationService gameTranslationService,
                                           IGameStatisticsService gameStatisticsService,
                                           IUpgradableSpecification upgradableSpecification,
                                           ICustomFormatCalculationService formatCalculator,
                                           IConfigService configService,
                                           IMapCoversToLocal coverMapper,
                                           IBroadcastSignalRMessage signalRBroadcaster)
            : base(signalRBroadcaster)
        {
            _gameService = gameService;
            _gameTranslationService = gameTranslationService;
            _gameStatisticsService = gameStatisticsService;
            _upgradableSpecification = upgradableSpecification;
            _formatCalculator = formatCalculator;
            _configService = configService;
            _coverMapper = coverMapper;
        }

        protected GameControllerWithSignalR(IGameService gameService,
                                           IUpgradableSpecification upgradableSpecification,
                                           ICustomFormatCalculationService formatCalculator,
                                           IBroadcastSignalRMessage signalRBroadcaster,
                                           string resource)
            : base(signalRBroadcaster)
        {
            _gameService = gameService;
            _upgradableSpecification = upgradableSpecification;
            _formatCalculator = formatCalculator;
        }

        protected override GameResource GetResourceById(int id)
        {
            var game = _gameService.GetGame(id);
            var resource = MapToResource(game);
            return resource;
        }

        protected GameResource MapToResource(Game game)
        {
            if (game == null)
            {
                return null;
            }

            var availDelay = _configService.AvailabilityDelay;
            var language = (Language)_configService.GameInfoLanguage;

            var translations = _gameTranslationService.GetAllTranslationsForGameMetadata(game.GameMetadataId);
            var translation = GetGameTranslation(translations, game.GameMetadata, language);

            var resource = game.ToResource(availDelay, translation, _upgradableSpecification, _formatCalculator);
            FetchAndLinkGameStatistics(resource);

            _coverMapper.ConvertToLocalUrls(resource.Id, resource.Images);

            return resource;
        }

        protected List<GameResource> MapToResource(List<Game> games)
        {
            var resources = new List<GameResource>();
            var availDelay = _configService.AvailabilityDelay;
            var language = (Language)_configService.GameInfoLanguage;

            foreach (var game in games)
            {
                if (game == null)
                {
                    continue;
                }

                var translations = _gameTranslationService.GetAllTranslationsForGameMetadata(game.GameMetadataId);
                var translation = GetGameTranslation(translations, game.GameMetadata, language);

                var resource = game.ToResource(availDelay, translation, _upgradableSpecification, _formatCalculator);
                FetchAndLinkGameStatistics(resource);

                resources.Add(resource);
            }

            return resources;
        }

        private GameTranslation GetGameTranslation(List<GameTranslation> translations, GameMetadata game, Language language)
        {
            if (language == Language.Original)
            {
                return new GameTranslation
                {
                    Title = game.OriginalTitle,
                    Overview = game.Overview
                };
            }

            return translations.FirstOrDefault(t => t.Language == language && t.GameMetadataId == game.Id);
        }

        private void FetchAndLinkGameStatistics(GameResource resource)
        {
            LinkGameStatistics(resource, _gameStatisticsService.GameStatistics(resource.Id));
        }

        private void LinkGameStatistics(GameResource resource, GameStatistics gameStatistics)
        {
            resource.Statistics = gameStatistics.ToResource();
            resource.HasFile = gameStatistics.GameFileCount > 0;
            resource.SizeOnDisk = gameStatistics.SizeOnDisk;
        }

        [NonAction]
        public void Handle(GameGrabbedEvent message)
        {
            var resource = MapToResource(message.Game.Game);
            resource.Grabbed = true;

            BroadcastResourceChange(ModelAction.Updated, resource);
        }

        [NonAction]
        public void Handle(GameFileImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.ImportedGame.Game.Id);
        }

        [NonAction]
        public void Handle(GameFileDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.GameFile.Game.Id);
        }
    }
}
