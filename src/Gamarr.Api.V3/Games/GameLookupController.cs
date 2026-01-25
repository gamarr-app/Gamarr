using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.RAWG;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using Gamarr.Http;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Games
{
    [V3ApiController("game/lookup")]
    public class GameLookupController : RestController<GameResource>
    {
        private readonly ISearchForNewGame _searchProxy;
        private readonly IProvideGameInfo _gameInfo;
        private readonly RawgProxy _rawgProxy;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly INamingConfigService _namingService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IConfigService _configService;
        private readonly IImportListExclusionService _importListExclusionService;

        public GameLookupController(ISearchForNewGame searchProxy,
                                 IProvideGameInfo gameInfo,
                                 RawgProxy rawgProxy,
                                 IBuildFileNames fileNameBuilder,
                                 INamingConfigService namingService,
                                 IMapCoversToLocal coverMapper,
                                 IConfigService configService,
                                 IImportListExclusionService importListExclusionService)
        {
            _gameInfo = gameInfo;
            _searchProxy = searchProxy;
            _rawgProxy = rawgProxy;
            _fileNameBuilder = fileNameBuilder;
            _namingService = namingService;
            _coverMapper = coverMapper;
            _configService = configService;
            _importListExclusionService = importListExclusionService;
        }

        [NonAction]
        public override ActionResult<GameResource> GetResourceByIdWithErrorHandler(int id)
        {
            throw new NotImplementedException();
        }

        protected override GameResource GetResourceById(int id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Lookup game by Steam App ID (primary identifier)
        /// </summary>
        [HttpGet("steam")]
        [Produces("application/json")]
        public GameResource SearchBySteamAppId(int steamAppId)
        {
            var availDelay = _configService.AvailabilityDelay;
            var metadata = _gameInfo.GetGameBySteamAppId(steamAppId);

            if (metadata == null)
            {
                return null;
            }

            var result = new Game { GameMetadata = metadata };
            var translation = metadata.Translations?.FirstOrDefault(t => t.Language == (Language)_configService.GameInfoLanguage);
            return result.ToResource(availDelay, translation);
        }

        /// <summary>
        /// Lookup game by IGDB ID (secondary identifier)
        /// </summary>
        [HttpGet("igdb")]
        [Produces("application/json")]
        public GameResource SearchByIgdbId(int igdbId)
        {
            var availDelay = _configService.AvailabilityDelay;
            var metadata = _gameInfo.GetGameInfo(igdbId);

            if (metadata == null)
            {
                return null;
            }

            var result = new Game { GameMetadata = metadata };
            var translation = metadata.Translations?.FirstOrDefault(t => t.Language == (Language)_configService.GameInfoLanguage);
            return result.ToResource(availDelay, translation);
        }

        /// <summary>
        /// Lookup game by RAWG ID
        /// </summary>
        [HttpGet("rawg")]
        [Produces("application/json")]
        public GameResource SearchByRawgId(int rawgId)
        {
            var availDelay = _configService.AvailabilityDelay;
            var metadata = _rawgProxy.GetGameInfo(rawgId);

            if (metadata == null)
            {
                return null;
            }

            var result = new Game { GameMetadata = metadata };
            var translation = metadata.Translations?.FirstOrDefault(t => t.Language == (Language)_configService.GameInfoLanguage);
            return result.ToResource(availDelay, translation);
        }

        [HttpGet]
        [Produces("application/json")]
        public IEnumerable<GameResource> Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewGame(term);

            return MapToResource(searchResults);
        }

        private IEnumerable<GameResource> MapToResource(IEnumerable<Game> games)
        {
            var gameInfoLanguage = (Language)_configService.GameInfoLanguage;
            var availDelay = _configService.AvailabilityDelay;
            var namingConfig = _namingService.GetConfig();

            var listExclusions = _importListExclusionService.All();

            foreach (var currentGame in games)
            {
                var translation = currentGame.GameMetadata.Value.Translations.FirstOrDefault(t => t.Language == gameInfoLanguage);
                var resource = currentGame.ToResource(availDelay, translation);

                _coverMapper.ConvertToLocalUrls(resource.Id, resource.Images);

                var poster = currentGame.GameMetadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);
                if (poster != null)
                {
                    resource.RemotePoster = poster.RemoteUrl;
                }

                resource.Folder = _fileNameBuilder.GetGameFolder(currentGame, namingConfig);

                // Check if excluded by Steam App ID (primary) or IGDB ID (secondary)
                resource.IsExcluded = listExclusions.Any(e =>
                    (resource.SteamAppId > 0 && e.SteamAppId == resource.SteamAppId) ||
                    (resource.IgdbId > 0 && e.IgdbId == resource.IgdbId));

                yield return resource;
            }
        }
    }
}
