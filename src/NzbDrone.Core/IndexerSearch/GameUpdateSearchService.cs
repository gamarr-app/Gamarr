using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public interface IGameUpdateSearchService
    {
        void SearchForUpdatesAndMissingDlc(int gameId, bool userInvokedSearch);
    }

    /// <summary>
    /// The first search for a newly added console game grabs the base release
    /// and the decision engine rejects every update release in the same result
    /// set — correctly, because no base game was on disk yet. Nothing used to
    /// look again: no handler reacted to an import with a search, RSS only
    /// reports new uploads so an update released years ago never reappears, and
    /// a game is never "missing" once its base file is there. The game sat on
    /// its base version until the user searched by hand, which is exactly what
    /// MonitorUpdates is supposed to avoid (#210).
    ///
    /// So: when a base file lands, queue a search for the game's updates and
    /// for its monitored DLC slots. The decision engine still has the last word
    /// on which update, if any, is taken — only a strictly newer one than the
    /// installed update is accepted (#203).
    /// </summary>
    public class GameUpdateSearchService : IGameUpdateSearchService,
                                           IHandle<GameFileAddedEvent>,
                                           IExecute<GameUpdateSearchCommand>
    {
        private readonly IGameService _gameService;
        private readonly IGameComponentService _componentService;
        private readonly IMediaFileService _mediaFileService;
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IGameSearchBackoff _searchBackoff;
        private readonly Logger _logger;

        // Guards both of the fields below. A library scan publishes one
        // GameFileAddedEvent per adopted file, from a different thread than the
        // executor that drains them.
        private readonly object _lock = new object();
        private readonly HashSet<int> _pending = new HashSet<int>();
        private bool _draining;

        public GameUpdateSearchService(IGameService gameService,
                                       IGameComponentService componentService,
                                       IMediaFileService mediaFileService,
                                       ISearchForReleases releaseSearchService,
                                       IProcessDownloadDecisions processDownloadDecisions,
                                       IManageCommandQueue commandQueueManager,
                                       IGameSearchBackoff searchBackoff,
                                       Logger logger)
        {
            _gameService = gameService;
            _componentService = componentService;
            _mediaFileService = mediaFileService;
            _releaseSearchService = releaseSearchService;
            _processDownloadDecisions = processDownloadDecisions;
            _commandQueueManager = commandQueueManager;
            _searchBackoff = searchBackoff;
            _logger = logger;
        }

        // Shared with the rest of the fork (GameFileUnit): updates live under
        // "Updates/", DLC under "DLC/", everything else — including a
        // folder-granularity release, whose RelativePath is empty — is base.
        internal static bool IsBaseUnit(GameFile file)
        {
            return GameFileUnit.IsBase(file);
        }

        internal static bool IsUpdateUnit(GameFile file)
        {
            return GameFileUnit.IsUpdate(file);
        }

        /// <summary>
        /// Fires for an auto-import, a manual import and a disk-scan adoption
        /// alike: all three add the file through MediaFileService.Add.
        ///
        /// Ordered last so the primary-file assignment in GameService has
        /// already run and the game row is current before the search is queued
        /// (#216).
        /// </summary>
        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(GameFileAddedEvent message)
        {
            // An exception from a handler skips every handler after it in the
            // same publish, so nothing in here is allowed to escape — and
            // nothing in here touches the disk or an indexer either. All this
            // does is note the game and queue the command that does the work.
            try
            {
                if (!IsBaseUnit(message.GameFile))
                {
                    return;
                }

                // Stored row first: the Game the importer carries is the copy it
                // loaded before the import and its GameFileId / monitoring can
                // already be out of date by the time this runs.
                var game = _gameService.FindGame(message.GameFile.GameId) ?? message.GameFile.Game;

                if (game == null)
                {
                    return;
                }

                if (!game.Monitored || !game.MonitorUpdates)
                {
                    return;
                }

                bool push;

                lock (_lock)
                {
                    // A multi-file import publishes an event per file. The game
                    // is worth one search however many files it brought.
                    if (!_pending.Add(game.Id))
                    {
                        return;
                    }

                    // A drain already running will take this game on its next
                    // lap; a second command would only duplicate the work.
                    push = !_draining;
                }

                if (push)
                {
                    _commandQueueManager.Push(new GameUpdateSearchCommand());
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to queue an update search for game [{0}]", message.GameFile?.GameId);
            }
        }

        public void Execute(GameUpdateSearchCommand message)
        {
            lock (_lock)
            {
                _draining = true;
            }

            try
            {
                while (true)
                {
                    List<int> batch;

                    lock (_lock)
                    {
                        if (_pending.Count == 0)
                        {
                            _draining = false;
                            break;
                        }

                        batch = _pending.ToList();
                        _pending.Clear();
                    }

                    // Deliberately serial, and deliberately one command for the
                    // whole batch: a first library scan can adopt a base file
                    // for every game at once, and a command per game would put
                    // every executor thread on an indexer search at the same
                    // time and spend the whole quota in a burst.
                    foreach (var gameId in batch)
                    {
                        SearchForUpdatesAndMissingDlc(gameId, false);
                    }
                }
            }
            catch (Exception)
            {
                lock (_lock)
                {
                    _draining = false;
                }

                throw;
            }
        }

        public void SearchForUpdatesAndMissingDlc(int gameId, bool userInvokedSearch)
        {
            // The id was captured when the file was imported and the queue can
            // sit for a while, so resolve the game rather than trust it.
            var game = _gameService.FindGame(gameId);

            if (game == null)
            {
                _logger.Debug("Game [{0}] was deleted before its update search ran, skipping", gameId);

                return;
            }

            if (!game.Monitored || !game.MonitorUpdates)
            {
                _logger.Debug("Updates are not monitored for {0}, skipping update search", game.Title);

                return;
            }

            var files = _mediaFileService.GetFilesByGame(gameId) ?? new List<GameFile>();

            if (!files.Any(IsBaseUnit))
            {
                _logger.Debug("{0} has no base game file, an update release could only be rejected; skipping", game.Title);

                return;
            }

            if (!userInvokedSearch && _searchBackoff.ShouldSkip(game))
            {
                return;
            }

            try
            {
                var decisions = _releaseSearchService.GameSearch(gameId, userInvokedSearch, false)
                                                     .GetAwaiter().GetResult();

                if (!userInvokedSearch || decisions.Count > 0)
                {
                    _searchBackoff.Record(gameId, decisions.Count);
                }

                // The base game is already on disk, so only the update releases
                // the first search had to reject are of interest. Handing the
                // base releases back to the processor would do nothing useful
                // and could re-grab the file we just imported.
                var updates = decisions
                    .Where(d => (d.RemoteGame?.ParsedGameInfo?.ContentType ?? ReleaseContentType.Unknown) == ReleaseContentType.UpdateOnly)
                    .ToList();

                _logger.ProgressInfo("Update search for {0} returned {1} reports, {2} of them updates", game.Title, decisions.Count, updates.Count);

                var processed = _processDownloadDecisions.ProcessDecisions(updates).GetAwaiter().GetResult();

                _logger.ProgressInfo("Completed update search for {0}. {1} reports downloaded.", game.Title, processed.Grabbed.Count);
            }
            catch (ModelNotFoundException ex)
            {
                // Same race the lookup above cannot close: the game can go away
                // between it and the lookup inside the search itself.
                _logger.Debug(ex, "Game [{0}] was deleted during its update search, skipping", gameId);

                return;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to search for updates to game [{0}]", gameId);
            }

            QueueMissingDlcSearches(game, files);
        }

        // A DLC needs its own indexer query — the release carries the DLC's
        // name, not just the game's — which GameComponentSearchCommand already
        // knows how to build. DLC slots default to unmonitored, so this only
        // covers the ones the user opted into, and the command queue drops a
        // slot that is already queued or running.
        private void QueueMissingDlcSearches(Game game, List<GameFile> files)
        {
            try
            {
                var linked = files.Select(f => f.ComponentId).ToHashSet();

                var missingDlc = (_componentService.GetByGame(game.Id) ?? new List<GameComponent>())
                    .Where(c => c.ComponentType == GameComponentType.Dlc && c.Monitored && !linked.Contains(c.Id))
                    .ToList();

                foreach (var slot in missingDlc)
                {
                    _commandQueueManager.Push(new GameComponentSearchCommand
                    {
                        GameId = game.Id,
                        ComponentId = slot.Id
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to queue DLC searches for {0}", game.Title);
            }
        }
    }
}
