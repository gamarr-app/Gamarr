using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Games;
using Gamarr.Api.V3.Games;
using Gamarr.Http;
using Gamarr.Http.Extensions;

namespace Gamarr.Api.V3.History
{
    [V3ApiController]
    public class HistoryController : Controller
    {
        private readonly IHistoryService _historyService;
        private readonly IGameService _gameService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IUpgradableSpecification _upgradableSpecification;
        private readonly IFailedDownloadService _failedDownloadService;

        public HistoryController(IHistoryService historyService,
                             IGameService gameService,
                             ICustomFormatCalculationService formatCalculator,
                             IUpgradableSpecification upgradableSpecification,
                             IFailedDownloadService failedDownloadService)
        {
            _historyService = historyService;
            _gameService = gameService;
            _formatCalculator = formatCalculator;
            _upgradableSpecification = upgradableSpecification;
            _failedDownloadService = failedDownloadService;
        }

        protected HistoryResource MapToResource(GameHistory model, bool includeGame)
        {
            if (model.Game == null)
            {
                model.Game = _gameService.GetGame(model.GameId);
            }

            var resource = model.ToResource(_formatCalculator);

            if (includeGame)
            {
                resource.Game = model.Game.ToResource(0);
            }

            if (model.Game != null)
            {
                resource.QualityCutoffNotMet = _upgradableSpecification.QualityCutoffNotMet(model.Game.QualityProfile, model.Quality);
            }

            return resource;
        }

        [HttpGet]
        [Produces("application/json")]
        public PagingResource<HistoryResource> GetHistory([FromQuery] PagingRequestResource paging, bool includeGame, [FromQuery(Name = "eventType")] int[] eventTypes, string downloadId, [FromQuery] int[] gameIds = null, [FromQuery] int[] languages = null, [FromQuery] int[] quality = null)
        {
            var pagingResource = new PagingResource<HistoryResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, GameHistory>(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "date",
                    "languages",
                    "gameMetadata.sortTitle",
                    "quality"
                },
                "date",
                SortDirection.Descending);

            if (eventTypes != null && eventTypes.Any())
            {
                 pagingSpec.FilterExpressions.Add(v => eventTypes.Contains((int)v.EventType));
            }

            if (downloadId.IsNotNullOrWhiteSpace())
            {
                pagingSpec.FilterExpressions.Add(h => h.DownloadId == downloadId);
            }

            if (gameIds != null && gameIds.Any())
            {
                pagingSpec.FilterExpressions.Add(h => gameIds.Contains(h.GameId));
            }

            return pagingSpec.ApplyToPage(h => _historyService.Paged(pagingSpec, languages, quality), h => MapToResource(h, includeGame));
        }

        [HttpGet("since")]
        [Produces("application/json")]
        public List<HistoryResource> GetHistorySince(DateTime date, GameHistoryEventType? eventType = null, bool includeGame = false)
        {
            return _historyService.Since(date, eventType).Select(h => MapToResource(h, includeGame)).ToList();
        }

        [HttpGet("game")]
        [Produces("application/json")]
        public List<HistoryResource> GetGameHistory(int gameId, GameHistoryEventType? eventType = null, bool includeGame = false)
        {
            return _historyService.GetByGameId(gameId, eventType).Select(h => MapToResource(h, includeGame)).ToList();
        }

        // skipRedownload and deleteFiles make this the single "this grab was bad, undo it"
        // action: blocklist the release (BlocklistService handles DownloadFailedEvent), remove
        // the download from the client (DownloadEventHub, once FailedDownloadService attaches
        // the TrackedDownload) and optionally delete what it already imported. Both default to
        // the previous behaviour, so an existing caller is unaffected.
        [HttpPost("failed/{id}")]
        public object MarkAsFailed([FromRoute] int id, [FromQuery] bool skipRedownload = false, [FromQuery] bool deleteFiles = false)
        {
            _failedDownloadService.MarkAsFailed(id, skipRedownload, deleteFiles);
            return new { };
        }
    }
}
