using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Validation;
using Gamarr.Http;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Gamarr.Api.V3.Indexers
{
    [V3ApiController]
    public class ReleaseController : ReleaseControllerBase
    {
        private readonly IFetchAndParseRss _rssFetcherAndParser;
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IMakeDownloadDecision _downloadDecisionMaker;
        private readonly IPrioritizeDownloadDecision _prioritizeDownloadDecision;
        private readonly IDownloadService _downloadService;
        private readonly IGameService _gameService;
        private readonly Logger _logger;

        private readonly ICached<RemoteGame> _remoteGameCache;

        public ReleaseController(IFetchAndParseRss rssFetcherAndParser,
                             ISearchForReleases releaseSearchService,
                             IMakeDownloadDecision downloadDecisionMaker,
                             IPrioritizeDownloadDecision prioritizeDownloadDecision,
                             IDownloadService downloadService,
                             IGameService gameService,
                             ICacheManager cacheManager,
                             IQualityProfileService qualityProfileService,
                             Logger logger)
            : base(qualityProfileService)
        {
            _rssFetcherAndParser = rssFetcherAndParser;
            _releaseSearchService = releaseSearchService;
            _downloadDecisionMaker = downloadDecisionMaker;
            _prioritizeDownloadDecision = prioritizeDownloadDecision;
            _downloadService = downloadService;
            _gameService = gameService;
            _logger = logger;

            PostValidator.RuleFor(s => s.IndexerId).ValidId();
            PostValidator.RuleFor(s => s.Guid).NotEmpty();

            _remoteGameCache = cacheManager.GetCache<RemoteGame>(GetType(), "remoteGames");
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<object> DownloadRelease([FromBody] ReleaseResource release)
        {
            var remoteGame = _remoteGameCache.Find(GetCacheKey(release));

            if (remoteGame == null)
            {
                _logger.Debug("Couldn't find requested release in cache, cache timeout probably expired.");

                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Couldn't find requested release in cache, try searching again");
            }

            try
            {
                if (release.ShouldOverride == true)
                {
                    Ensure.That(release.GameId, () => release.GameId).IsNotNull();
                    Ensure.That(release.Quality, () => release.Quality).IsNotNull();
                    Ensure.That(release.Languages, () => release.Languages).IsNotNull();

                    // Clone the remote episode so we don't overwrite anything on the original
                    remoteGame = new RemoteGame
                    {
                        Release = remoteGame.Release,
                        ParsedGameInfo = remoteGame.ParsedGameInfo.JsonClone(),
                        GameRequested = remoteGame.GameRequested,
                        DownloadAllowed = remoteGame.DownloadAllowed,
                        SeedConfiguration = remoteGame.SeedConfiguration,
                        CustomFormats = remoteGame.CustomFormats,
                        CustomFormatScore = remoteGame.CustomFormatScore,
                        GameMatchType = remoteGame.GameMatchType,
                        ReleaseSource = remoteGame.ReleaseSource
                    };

                    remoteGame.Game = _gameService.GetGame(release.GameId!.Value);
                    remoteGame.ParsedGameInfo.Quality = release.Quality;
                    remoteGame.Languages = release.Languages;
                }

                if (remoteGame.Game == null)
                {
                    if (release.GameId.HasValue)
                    {
                        var game = _gameService.GetGame(release.GameId.Value);

                        remoteGame.Game = game;
                    }
                    else
                    {
                        throw new NzbDroneClientException(HttpStatusCode.NotFound, "Unable to find matching game, will need to be manually provided");
                    }
                }

                await _downloadService.DownloadReport(remoteGame, release.DownloadClientId);
            }
            catch (ReleaseDownloadException ex)
            {
                _logger.Error(ex, ex.Message);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Getting release from indexer failed");
            }

            return release;
        }

        [HttpGet]
        [Produces("application/json")]
        public async Task<List<ReleaseResource>> GetReleases(int? gameId)
        {
            if (gameId.HasValue)
            {
                return await GetGameReleases(gameId.Value);
            }

            return await GetRss();
        }

        private async Task<List<ReleaseResource>> GetGameReleases(int gameId)
        {
            try
            {
                var decisions = await _releaseSearchService.GameSearch(gameId, true, true);
                var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisionsForGames(decisions);

                return MapDecisions(prioritizedDecisions);
            }
            catch (SearchFailedException ex)
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Game search failed: " + ex.Message);
                throw new NzbDroneClientException(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private async Task<List<ReleaseResource>> GetRss()
        {
            var reports = await _rssFetcherAndParser.Fetch();
            var decisions = _downloadDecisionMaker.GetRssDecision(reports);
            var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisionsForGames(decisions);

            return MapDecisions(prioritizedDecisions);
        }

        protected override ReleaseResource MapDecision(DownloadDecision decision, int initialWeight)
        {
            var resource = base.MapDecision(decision, initialWeight);
            _remoteGameCache.Set(GetCacheKey(resource), decision.RemoteGame, TimeSpan.FromMinutes(30));

            return resource;
        }

        private string GetCacheKey(ReleaseResource resource)
        {
            return string.Concat(resource.IndexerId, "_", resource.Guid);
        }
    }
}
