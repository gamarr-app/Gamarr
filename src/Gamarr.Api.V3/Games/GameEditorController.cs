using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Translations;
using Gamarr.Http;

namespace Gamarr.Api.V3.Games
{
    [V3ApiController("game/editor")]
    public class GameEditorController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IConfigService _configService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly GameEditorValidator _gameEditorValidator;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public GameEditorController(IGameService gameService,
            IGameTranslationService gameTranslationService,
            IMapCoversToLocal coverMapper,
            IConfigService configService,
            IManageCommandQueue commandQueueManager,
            GameEditorValidator gameEditorValidator,
            IUpgradableSpecification upgradableSpecification)
        {
            _gameService = gameService;
            _gameTranslationService = gameTranslationService;
            _coverMapper = coverMapper;
            _configService = configService;
            _commandQueueManager = commandQueueManager;
            _gameEditorValidator = gameEditorValidator;
            _upgradableSpecification = upgradableSpecification;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] GameEditorResource resource)
        {
            var gamesToUpdate = _gameService.GetGames(resource.GameIds);
            var gamesToMove = new List<BulkMoveGame>();

            foreach (var game in gamesToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    game.Monitored = resource.Monitored.Value;
                }

                if (resource.QualityProfileId.HasValue)
                {
                    game.QualityProfileId = resource.QualityProfileId.Value;
                }

                if (resource.MinimumAvailability.HasValue)
                {
                    game.MinimumAvailability = resource.MinimumAvailability.Value;
                }

                if (resource.RootFolderPath.IsNotNullOrWhiteSpace())
                {
                    game.RootFolderPath = resource.RootFolderPath;
                    gamesToMove.Add(new BulkMoveGame
                    {
                        GameId = game.Id,
                        SourcePath = game.Path
                    });
                }

                if (resource.Tags != null)
                {
                    var newTags = resource.Tags;
                    var applyTags = resource.ApplyTags;

                    switch (applyTags)
                    {
                        case ApplyTags.Add:
                            newTags.ForEach(t => game.Tags.Add(t));
                            break;
                        case ApplyTags.Remove:
                            newTags.ForEach(t => game.Tags.Remove(t));
                            break;
                        case ApplyTags.Replace:
                            game.Tags = new HashSet<int>(newTags);
                            break;
                    }
                }

                var validationResult = _gameEditorValidator.Validate(game);

                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }
            }

            if (resource.MoveFiles && gamesToMove.Any())
            {
                _commandQueueManager.Push(new BulkMoveGameCommand
                {
                    DestinationRootFolder = resource.RootFolderPath,
                    Games = gamesToMove
                });
            }

            var configLanguage = (Language)_configService.GameInfoLanguage;
            var availabilityDelay = _configService.AvailabilityDelay;

            var translations = _gameTranslationService.GetAllTranslationsForLanguage(configLanguage);
            var tdict = translations.ToDictionaryIgnoreDuplicates(x => x.GameMetadataId);

            var updatedGames = _gameService.UpdateGame(gamesToUpdate, !resource.MoveFiles);

            var gamesResources = new List<GameResource>(updatedGames.Count);

            foreach (var game in updatedGames)
            {
                var translation = GetTranslationFromDict(tdict, game.GameMetadata, configLanguage);
                var gameResource = game.ToResource(availabilityDelay, translation, _upgradableSpecification);

                MapCoversToLocal(gameResource);

                gamesResources.Add(gameResource);
            }

            return Accepted(gamesResources);
        }

        [HttpDelete]
        public object DeleteGames([FromBody] GameEditorResource resource)
        {
            _gameService.DeleteGames(resource.GameIds, resource.DeleteFiles, resource.AddImportExclusion);

            return new { };
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

        private void MapCoversToLocal(GameResource game)
        {
            _coverMapper.ConvertToLocalUrls(game.Id, game.Images);
        }
    }
}
