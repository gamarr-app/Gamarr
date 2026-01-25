using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Games;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.IndexerSearch
{
    public class GameSearchService : IExecute<GamesSearchCommand>, IExecute<MoviesSearchCommand>, IExecute<MissingGamesSearchCommand>, IExecute<CutoffUnmetGamesSearchCommand>
    {
        private readonly IGameService _gameService;
        private readonly IGameCutoffService _gameCutoffService;
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly IQueueService _queueService;
        private readonly Logger _logger;

        public GameSearchService(IGameService gameService,
                                   IGameCutoffService gameCutoffService,
                                   ISearchForReleases releaseSearchService,
                                   IProcessDownloadDecisions processDownloadDecisions,
                                   IQueueService queueService,
                                   Logger logger)
        {
            _gameService = gameService;
            _gameCutoffService = gameCutoffService;
            _releaseSearchService = releaseSearchService;
            _processDownloadDecisions = processDownloadDecisions;
            _queueService = queueService;
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

            var queue = _queueService.GetQueue().Where(q => q.Game != null).Select(q => q.Game.Id);
            var missing = games.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkGames(missing, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
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

        private async Task SearchForBulkGames(List<Game> games, bool userInvokedSearch)
        {
            _logger.ProgressInfo("Performing search for {0} games", games.Count);
            var downloadedCount = 0;

            foreach (var gameId in games.GroupBy(e => e.Id).OrderBy(g => g.Min(m => m.LastSearchTime ?? DateTime.MinValue)))
            {
                List<DownloadDecision> decisions;

                try
                {
                    decisions = await _releaseSearchService.GameSearch(gameId.Key, userInvokedSearch, false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to search for game: [{0}]", gameId.Key);
                    continue;
                }

                var processDecisions = await _processDownloadDecisions.ProcessDecisions(decisions);

                downloadedCount += processDecisions.Grabbed.Count;
            }

            _logger.ProgressInfo("Completed search for {0} games. {1} reports downloaded.", games.Count, downloadedCount);
        }
    }
}
