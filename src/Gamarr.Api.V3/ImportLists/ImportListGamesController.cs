using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Organizer;
using Gamarr.Api.V3.Games;
using Gamarr.Http;

namespace Gamarr.Api.V3.ImportLists
{
    [V3ApiController("importlist/game")]
    public class ImportListGamesController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IAddGameService _addGameService;
        private readonly IProvideGameInfo _gameInfo;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IImportListGameService _listGameService;
        private readonly IImportListFactory _importListFactory;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly INamingConfigService _namingService;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public ImportListGamesController(IGameService gameService,
                                    IAddGameService addGameService,
                                    IProvideGameInfo gameInfo,
                                    IBuildFileNames fileNameBuilder,
                                    IImportListGameService listGameService,
                                    IImportListFactory importListFactory,
                                    IImportListExclusionService importListExclusionService,
                                    INamingConfigService namingService,
                                    IGameTranslationService gameTranslationService,
                                    IConfigService configService,
                                    Logger logger)
        {
            _gameService = gameService;
            _addGameService = addGameService;
            _gameInfo = gameInfo;
            _fileNameBuilder = fileNameBuilder;
            _listGameService = listGameService;
            _importListFactory = importListFactory;
            _importListExclusionService = importListExclusionService;
            _namingService = namingService;
            _gameTranslationService = gameTranslationService;
            _configService = configService;
            _logger = logger;
        }

        [HttpGet]
        public object GetDiscoverGames(bool includeRecommendations = false, bool includeTrending = false, bool includePopular = false)
        {
            var realResults = new List<ImportListGamesResource>();

            try
            {
                var gameLanguage = (Language)_configService.GameInfoLanguage;
                var listExclusions = _importListExclusionService.All();
                var existingIgdbIds = _gameService.AllGameIgdbIds();

                if (includeRecommendations)
                {
                    try
                    {
                        var recommendedResults = _gameService.GetRecommendedIgdbIds();

                        if (recommendedResults.Count > 0)
                        {
                            var mapped = _gameInfo.GetBulkGameInfo(recommendedResults).Select(m => new Game { GameMetadata = m }).ToList();

                            realResults.AddRange(MapToResource(mapped.Where(x => x != null), gameLanguage, isRecommendation: true));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to get recommended games");
                    }
                }

                if (includeTrending)
                {
                    try
                    {
                        var trendingResults = _gameInfo.GetTrendingGames();

                        realResults.AddRange(MapToResource(trendingResults.Select(m => new Game { GameMetadata = m }).Where(x => x != null), gameLanguage, isTrending: true));
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to get trending games");
                    }
                }

                if (includePopular)
                {
                    try
                    {
                        var popularResults = _gameInfo.GetPopularGames();

                        realResults.AddRange(MapToResource(popularResults.Select(m => new Game { GameMetadata = m }).Where(x => x != null), gameLanguage, isPopular: true));
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to get popular games");
                    }
                }

                // Add List Games
                try
                {
                    var listGames = MapToResource(_listGameService.GetAllForLists(_importListFactory.Enabled().Select(x => x.Definition.Id).ToList()), gameLanguage).ToList();

                    realResults.AddRange(listGames);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get import list games");
                }

                var groupedListGames = realResults.GroupBy(x => x.IgdbId);

                // Distinct Games
                realResults = groupedListGames.Select(x =>
                {
                    var game = x.First();

                    game.Lists = x.SelectMany(m => m.Lists).ToHashSet();
                    game.IsExcluded = listExclusions.Any(e => e.IgdbId == game.IgdbId);
                    game.IsExisting = existingIgdbIds.Any(e => e == game.IgdbId);
                    game.IsRecommendation = x.Any(m => m.IsRecommendation);
                    game.IsPopular = x.Any(m => m.IsPopular);
                    game.IsTrending = x.Any(m => m.IsTrending);

                    return game;
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get discover games");
            }

            return realResults;
        }

        [HttpPost]
        public object AddGames([FromBody] List<GameResource> resource)
        {
            var newGames = resource.ToModel();

            return _addGameService.AddGames(newGames, true).ToResource(0);
        }

        private IEnumerable<ImportListGamesResource> MapToResource(IEnumerable<Game> games, Language language, bool isRecommendation = false, bool isTrending = false, bool isPopular = false)
        {
            // Avoid calling for naming spec on every game in filenamebuilder
            var namingConfig = _namingService.GetConfig();

            foreach (var currentGame in games)
            {
                var resource = currentGame.ToResource();

                var poster = currentGame.GameMetadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);
                if (poster != null)
                {
                    resource.RemotePoster = poster.RemoteUrl;
                }

                var translation = currentGame.GameMetadata.Value.Translations.FirstOrDefault(t => t.Language == language);

                resource.Title = translation?.Title ?? resource.Title;
                resource.Overview = translation?.Overview ?? resource.Overview;
                resource.Folder = _fileNameBuilder.GetGameFolder(currentGame, namingConfig);
                resource.IsRecommendation = isRecommendation;
                resource.IsTrending = isTrending;
                resource.IsPopular = isPopular;

                yield return resource;
            }
        }

        private IEnumerable<ImportListGamesResource> MapToResource(IEnumerable<ImportListGame> games, Language language)
        {
            // Avoid calling for naming spec on every game in filenamebuilder
            var namingConfig = _namingService.GetConfig();

            var translations = _gameTranslationService
                .GetAllTranslationsForLanguage(language)
                .ToDictionary(x => x.GameMetadataId);

            foreach (var currentGame in games)
            {
                var resource = currentGame.ToResource();

                var poster = currentGame.GameMetadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);
                if (poster != null)
                {
                    resource.RemotePoster = poster.RemoteUrl;
                }

                var translation = GetTranslationFromDictionary(translations, currentGame.GameMetadata, language);

                resource.Title = translation?.Title ?? resource.Title;
                resource.Overview = translation?.Overview ?? resource.Overview;
                resource.Folder = _fileNameBuilder.GetGameFolder(new Game
                {
                    GameMetadata = currentGame.GameMetadata
                }, namingConfig);

                yield return resource;
            }
        }

        private GameTranslation GetTranslationFromDictionary(Dictionary<int, GameTranslation> translations, GameMetadata game, Language configLanguage)
        {
            if (configLanguage == Language.Original)
            {
                return new GameTranslation
                {
                    Title = game.OriginalTitle,
                    Overview = game.Overview
                };
            }

            translations.TryGetValue(game.Id, out var translation);

            return translation;
        }
    }
}
