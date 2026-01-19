using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.GameStats;
using NzbDrone.SignalR;
using Gamarr.Api.V3.Games;
using Gamarr.Http;
using Gamarr.Http.Extensions;

namespace Gamarr.Api.V3.Wanted
{
    [V3ApiController("wanted/cutoff")]
    public class CutoffController : GameControllerWithSignalR
    {
        private readonly IGameCutoffService _gameCutoffService;

        public CutoffController(IGameCutoffService gameCutoffService,
                            IGameService gameService,
                            IGameTranslationService gameTranslationService,
                            IGameStatisticsService gameStatisticsService,
                            IUpgradableSpecification upgradableSpecification,
                            ICustomFormatCalculationService formatCalculator,
                            IConfigService configService,
                            IMapCoversToLocal coverMapper,
                            IBroadcastSignalRMessage signalRBroadcaster)
            : base(gameService, gameTranslationService, gameStatisticsService, upgradableSpecification, formatCalculator, configService, coverMapper, signalRBroadcaster)
        {
            _gameCutoffService = gameCutoffService;
        }

        [NonAction]
        public override ActionResult<GameResource> GetResourceByIdWithErrorHandler(int id)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Produces("application/json")]
        public PagingResource<GameResource> GetCutoffUnmetGames([FromQuery] PagingRequestResource paging, bool monitored = true)
        {
            var pagingResource = new PagingResource<GameResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<GameResource, Game>(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "gameMetadata.digitalRelease",
                    "gameMetadata.inCinemas",
                    "gameMetadata.physicalRelease",
                    "gameMetadata.sortTitle",
                    "gameMetadata.year",
                    "games.lastSearchTime"
                },
                "gameMetadata.sortTitle",
                SortDirection.Ascending);

            pagingSpec.FilterExpressions.Add(v => v.Monitored == monitored);

            var resource = pagingSpec.ApplyToPage(_gameCutoffService.GamesWhereCutoffUnmet, v => MapToResource(v));

            return resource;
        }
    }
}
