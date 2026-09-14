using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download
{
    // Nothing used to stop a release that cannot be downloaded from being picked again. Every RSS
    // sync and every scheduled search re-ran the same decision, got the same approved release and
    // attempted the same grab, forever: one release on a rate-limited indexer produced ~190 failed
    // download requests in 16 hours and only stopped when a human unmonitored the game.
    //
    // The cap is counted from history rather than held in memory because the retry loop survives
    // restarts, and it blocklists rather than keeping a private cooldown table because the
    // blocklist already exists, is already consulted by the decision engine (BlocklistSpecification
    // rejects permanently, so the release stops being selected at all rather than being selected
    // and skipped), is already visible in the UI, and is already removable by the user. A private
    // cooldown would have been a second, invisible mechanism doing the same job worse.
    public class FailedGrabService : IHandleAsync<GameGrabFailedEvent>
    {
        // Deliberately more than one. A single failure is routinely transient - an indexer hiccup,
        // a download client restart - and blocklisting on the first one would be worse than the
        // bug. Three attempts of the identical release is no longer a hiccup.
        public const int MaximumGrabAttempts = 3;

        private readonly IHistoryService _historyService;
        private readonly IBlocklistService _blocklistService;
        private readonly Logger _logger;

        public FailedGrabService(IHistoryService historyService,
                                 IBlocklistService blocklistService,
                                 Logger logger)
        {
            _historyService = historyService;
            _blocklistService = blocklistService;
            _logger = logger;
        }

        public void HandleAsync(GameGrabFailedEvent message)
        {
            var remoteGame = message.Game;

            // History is keyed by game and the blocklist is keyed by game, so a release that never
            // matched one cannot be counted or blocked. It also cannot be re-selected, so there is
            // no loop to break.
            if (remoteGame?.Game == null || remoteGame.Game.Id <= 0 || remoteGame.Release == null)
            {
                return;
            }

            if (remoteGame.ParsedGameInfo == null)
            {
                _logger.Debug("Release '{0}' has no parsed info, cannot blocklist it after a failed grab.", remoteGame.Release.Title);

                return;
            }

            var guid = remoteGame.Release.Guid;

            if (guid.IsNullOrWhiteSpace())
            {
                _logger.Debug("Release '{0}' has no guid, cannot count its failed grabs.", remoteGame.Release.Title);

                return;
            }

            if (_blocklistService.Blocklisted(remoteGame.Game.Id, remoteGame.Release))
            {
                return;
            }

            // HistoryService writes its GrabFailed row from a synchronous IHandle, and the event
            // aggregator runs every synchronous handler to completion before it starts any
            // asynchronous one, so the failure being handled here is already counted below. That
            // ordering is the reason this is IHandleAsync and not IHandle.
            var failures = _historyService.GetByGameId(remoteGame.Game.Id, GameHistoryEventType.GrabFailed)
                                          .Count(h => guid == GetGuid(h));

            if (failures < MaximumGrabAttempts)
            {
                _logger.Debug("Release '{0}' from indexer {1} has failed to grab {2} of {3} times.", remoteGame.Release.Title, remoteGame.Release.Indexer, failures, MaximumGrabAttempts);

                return;
            }

            _logger.Warn("Release '{0}' from indexer {1} failed to grab {2} times, blocklisting it so it is not selected again.", remoteGame.Release.Title, remoteGame.Release.Indexer, failures);

            _blocklistService.Block(remoteGame, $"Grab failed {failures} times, most recently: {message.Reason}");
        }

        // HistoryService writes this key as "Guid", but the Data column is serialised camelCased,
        // so a row read back from the database has "guid" - and the dictionary that comes back
        // from deserialisation does not necessarily carry the case-insensitive comparer the
        // GameHistory constructor sets up. Matching on either casing explicitly is the difference
        // between this cap working and it silently counting zero failures forever.
        private static string GetGuid(GameHistory history)
        {
            if (history?.Data == null)
            {
                return null;
            }

            foreach (var pair in history.Data)
            {
                if (string.Equals(pair.Key, "Guid", StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return null;
        }
    }
}
