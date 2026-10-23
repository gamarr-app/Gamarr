using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.IndexerSearch
{
    public class GameSearchService : IExecute<GamesSearchCommand>, IExecute<MoviesSearchCommand>, IExecute<MissingGamesSearchCommand>, IExecute<CutoffUnmetGamesSearchCommand>, IExecute<GameComponentSearchCommand>
    {
        // A library whose games have never had an update imported is every game
        // in it, and sweeping all of them in one pass is the indexer stampede
        // this cap exists to prevent. Nothing is dropped: the oldest-searched
        // games go first, so successive passes drain the backlog.
        private const int MaxUpdateSweepPerPass = 50;

        private readonly IGameService _gameService;
        private readonly IGameCutoffService _gameCutoffService;
        private readonly IGameComponentService _componentService;
        private readonly IMediaFileService _mediaFileService;
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly IQueueService _queueService;
        private readonly IGameSearchBackoff _searchBackoff;
        private readonly IGameUpdateSearchService _updateSearchService;
        private readonly Logger _logger;

        public GameSearchService(IGameService gameService,
                                   IGameCutoffService gameCutoffService,
                                   IGameComponentService componentService,
                                   IMediaFileService mediaFileService,
                                   ISearchForReleases releaseSearchService,
                                   IProcessDownloadDecisions processDownloadDecisions,
                                   IQueueService queueService,
                                   IGameSearchBackoff searchBackoff,
                                   IGameUpdateSearchService updateSearchService,
                                   Logger logger)
        {
            _gameService = gameService;
            _gameCutoffService = gameCutoffService;
            _componentService = componentService;
            _mediaFileService = mediaFileService;
            _releaseSearchService = releaseSearchService;
            _processDownloadDecisions = processDownloadDecisions;
            _queueService = queueService;
            _searchBackoff = searchBackoff;
            _updateSearchService = updateSearchService;
            _logger = logger;
        }

        public void Execute(GamesSearchCommand message)
        {
            var userInvokedSearch = message.Trigger == CommandTrigger.Manual;

            var games = _gameService.GetGames(message.GameIds)
                .Where(m => (m.Monitored && m.IsAvailable()) || userInvokedSearch)
                .ToList();

            SearchForBulkGames(games, userInvokedSearch).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Radarr-compatible alias for GamesSearchCommand.
        /// Allows tools like Decluttarr to trigger searches using Radarr's API format.
        /// </summary>
        public void Execute(MoviesSearchCommand message)
        {
            var userInvokedSearch = message.Trigger == CommandTrigger.Manual;

            var games = _gameService.GetGames(message.MovieIds)
                .Where(m => (m.Monitored && m.IsAvailable()) || userInvokedSearch)
                .ToList();

            SearchForBulkGames(games, userInvokedSearch).GetAwaiter().GetResult();
        }

        public void Execute(MissingGamesSearchCommand message)
        {
            var pagingSpec = new PagingSpec<Game>
            {
                Page = 1,
                PageSize = 100000,
                SortDirection = SortDirection.Ascending,
                SortKey = "Id"
            };

            pagingSpec.FilterExpressions.Add(v => v.Monitored == true);

            var games = _gameService.GamesWithoutFiles(pagingSpec).Records.ToList();

            var queue = _queueService.GetQueue().Where(q => q.Game != null).Select(q => q.Game.Id).ToList();
            var missing = games.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkGames(missing, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();

            // Monitored DLC slots without a file are missing too; games-only
            // enumeration never sees them because their game has its base.
            var missingDlc = _componentService.GetMonitoredMissingDlc()
                                              .Where(c => !queue.Contains(c.GameId))
                                              .ToList();

            if (missingDlc.Any())
            {
                _logger.ProgressInfo("Performing search for {0} missing DLC component(s)", missingDlc.Count);

                foreach (var slot in missingDlc)
                {
                    SearchForComponent(slot, message.Trigger == CommandTrigger.Manual);
                }
            }

            SearchForGamesWithNoUpdateInstalled(queue, message.Trigger == CommandTrigger.Manual);
        }

        // A game with a base file and no update of any kind has never had an
        // update imported, and nothing else will ever surface one for it: RSS
        // only reports new uploads, so an update released before the game was
        // added is invisible to it, and the game stops being "missing" the
        // moment its base file arrives (#210). Once a game does carry an
        // update, only a newer one would upgrade it — and a newer one is a new
        // upload, which RSS does see — so this set is self-draining.
        private void SearchForGamesWithNoUpdateInstalled(List<int> queue, bool userInvokedSearch)
        {
            var games = (_gameService.GetAllGames() ?? new List<Game>())
                .Where(g => g.Monitored && g.MonitorUpdates && !queue.Contains(g.Id))
                .ToList();

            if (!games.Any())
            {
                return;
            }

            var filesByGame = (_mediaFileService.GetFilesByGames(games.Select(g => g.Id)) ?? new List<GameFile>())
                .GroupBy(f => f.GameId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var candidates = games
                .Where(g => filesByGame.TryGetValue(g.Id, out var files) &&
                            files.Any(GameUpdateSearchService.IsBaseUnit) &&
                            !files.Any(GameUpdateSearchService.IsUpdateUnit))
                .Where(g => userInvokedSearch || !_searchBackoff.ShouldSkip(g))
                .OrderBy(g => g.LastSearchTime ?? DateTime.MinValue)
                .Take(MaxUpdateSweepPerPass)
                .ToList();

            if (!candidates.Any())
            {
                return;
            }

            _logger.ProgressInfo("Performing update search for {0} game(s) with no update installed", candidates.Count);

            foreach (var game in candidates)
            {
                _updateSearchService.SearchForUpdatesAndMissingDlc(game.Id, userInvokedSearch);
            }
        }

        public void Execute(CutoffUnmetGamesSearchCommand message)
        {
            var pagingSpec = new PagingSpec<Game>
            {
                Page = 1,
                PageSize = 100000,
                SortDirection = SortDirection.Ascending,
                SortKey = "Id"
            };

            pagingSpec.FilterExpressions.Add(v => v.Monitored == true);

            var games = _gameCutoffService.GamesWhereCutoffUnmet(pagingSpec).Records.ToList();

            var queue = _queueService.GetQueue().Where(q => q.Game != null).Select(q => q.Game.Id);
            var missing = games.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkGames(missing, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }

        public void Execute(GameComponentSearchCommand message)
        {
            var component = _componentService.Get(message.ComponentId);

            SearchForComponent(component, message.Trigger == CommandTrigger.Manual);
        }

        private void SearchForComponent(GameComponent component, bool userInvokedSearch)
        {
            try
            {
                var decisions = _releaseSearchService.GameComponentSearch(component.GameId, component.Id, userInvokedSearch, false)
                                                     .GetAwaiter().GetResult();

                var scoped = FilterDecisionsToComponent(decisions, component);

                _logger.ProgressInfo("Component search for {0} returned {1} reports, {2} match the component", component.Title, decisions.Count, scoped.Count);

                var processed = _processDownloadDecisions.ProcessDecisions(scoped).GetAwaiter().GetResult();

                _logger.ProgressInfo("Completed component search for {0}. {1} reports downloaded.", component.Title, processed.Grabbed.Count);
            }
            catch (ModelNotFoundException ex)
            {
                // Same race as the bulk loop: the component's game can go away
                // mid-search, and the component goes with it.
                _logger.Debug(ex, "Game [{0}] was deleted during the component search, skipping {1}", component.GameId, component.Title);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to search for component: {0}", component.Title);
            }
        }

        // A game-wide search returns base, update and DLC releases mixed
        // together; only auto-grab the ones that fill the requested slot.
        internal static List<DownloadDecision> FilterDecisionsToComponent(List<DownloadDecision> decisions, GameComponent component)
        {
            return decisions.Where(d =>
            {
                var contentType = d.RemoteGame?.ParsedGameInfo?.ContentType ?? ReleaseContentType.Unknown;

                switch (component.ComponentType)
                {
                    case GameComponentType.Base:
                        return contentType.IncludesBaseGame();

                    case GameComponentType.Update:
                        return contentType == ReleaseContentType.UpdateOnly;

                    case GameComponentType.Dlc:
                        return contentType.OccupiesDlcSlot() && ReleaseMatchesDlcTitle(d, component);

                    default:
                        return false;
                }
            }).ToList();
        }

        private static bool ReleaseMatchesDlcTitle(DownloadDecision decision, GameComponent component)
        {
            // The game's title is what both sides share, so handing it over lets
            // the matcher compare the slot's own name against the release's
            // remainder rather than needing the slot's whole title verbatim
            // (#212).
            return GameComponentMatcher.ReleaseMatchesDlcTitle(decision.RemoteGame?.Release?.Title, component.Title, decision.RemoteGame?.Game?.Title);
        }

        private async Task SearchForBulkGames(List<Game> games, bool userInvokedSearch)
        {
            // A user who pressed Search means now, not "unless we looked recently".
            var searchable = userInvokedSearch ? games : games.Where(g => !_searchBackoff.ShouldSkip(g)).ToList();
            var skipped = games.Count - searchable.Count;

            if (skipped > 0)
            {
                _logger.ProgressInfo("Performing search for {0} games ({1} skipped, still backed off after finding nothing)", searchable.Count, skipped);
            }
            else
            {
                _logger.ProgressInfo("Performing search for {0} games", searchable.Count);
            }

            var downloadedCount = 0;

            foreach (var gameId in searchable.GroupBy(e => e.Id).OrderBy(g => g.Min(m => m.LastSearchTime ?? DateTime.MinValue)))
            {
                List<DownloadDecision> decisions;

                // The list was loaded when the command started; a long search
                // gives the user plenty of time to delete a game still in it.
                // That is a skip, not a failure — see the catch below for the
                // narrower race this check cannot close.
                if (_gameService.FindGame(gameId.Key) == null)
                {
                    _logger.Debug("Game [{0}] was deleted during the search, skipping", gameId.Key);
                    continue;
                }

                try
                {
                    decisions = await _releaseSearchService.GameSearch(gameId.Key, userInvokedSearch, false);
                }
                catch (ModelNotFoundException ex)
                {
                    _logger.Debug(ex, "Game [{0}] was deleted during the search, skipping", gameId.Key);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to search for game: [{0}]", gameId.Key);
                    continue;
                }

                // Only automatic passes escalate. A manual search that finds
                // nothing still clears an existing backoff if it finds something,
                // but must not deepen one the user is trying to search past.
                if (!userInvokedSearch || decisions.Count > 0)
                {
                    _searchBackoff.Record(gameId.Key, decisions.Count);
                }

                var processDecisions = await _processDownloadDecisions.ProcessDecisions(decisions);

                downloadedCount += processDecisions.Grabbed.Count;
            }

            _logger.ProgressInfo("Completed search for {0} games. {1} reports downloaded.", searchable.Count, downloadedCount);
        }
    }
}
