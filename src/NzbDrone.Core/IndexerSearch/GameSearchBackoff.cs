using System;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.IndexerSearch
{
    public interface IGameSearchBackoff
    {
        bool ShouldSkip(Game game);
        void Record(int gameId, int decisionCount);
    }

    // A game that no indexer carries gets searched again on every automatic pass,
    // forever, and each pass spends indexer quota. Worse, a rate-limited indexer
    // does not report an error - it answers with an empty result set - so a search
    // that found nothing because the quota was gone is indistinguishable from one
    // that found nothing because nothing exists, and searching again is what keeps
    // the quota gone. Backing off on repeatedly-empty searches is correct in both
    // cases: either the release still is not there, or we are the reason we cannot
    // see it.
    //
    // The escalation counter is deliberately in-memory only. LastSearchTime is
    // already persisted per game, so the *window* survives a restart; only the
    // escalation level resets, which costs one extra search per game after a
    // restart and never skips a search it should have run.
    public class GameSearchBackoff : IGameSearchBackoff
    {
        // 1h, 2h, 4h ... capped. Deliberately starts short: one empty search is
        // weak evidence, and a release that appears an hour after we looked is
        // the normal case this must not delay much.
        private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(24);

        private readonly ICached<int> _consecutiveEmpty;
        private readonly Logger _logger;

        public GameSearchBackoff(ICacheManager cacheManager, Logger logger)
        {
            _consecutiveEmpty = cacheManager.GetCache<int>(GetType());
            _logger = logger;
        }

        public bool ShouldSkip(Game game)
        {
            var lastSearch = game.LastSearchTime;

            if (lastSearch == null)
            {
                return false;
            }

            var backoff = BackoffFor(_consecutiveEmpty.Find(game.Id.ToString()));

            if (backoff == TimeSpan.Zero)
            {
                return false;
            }

            var nextSearch = lastSearch.Value + backoff;

            if (nextSearch <= DateTime.UtcNow)
            {
                return false;
            }

            _logger.Debug("Skipping search for [{0}] {1}: last {2} searches found nothing, next search after {3:u}",
                game.Id,
                game.Title,
                _consecutiveEmpty.Find(game.Id.ToString()),
                nextSearch);

            return true;
        }

        public void Record(int gameId, int decisionCount)
        {
            var key = gameId.ToString();

            // Any decision at all - even one we reject - means an indexer
            // answered with something about this game, so the run of empties is
            // broken and the next automatic pass should search normally.
            if (decisionCount > 0)
            {
                _consecutiveEmpty.Remove(key);
                return;
            }

            var count = _consecutiveEmpty.Find(key) + 1;
            _consecutiveEmpty.Set(key, count);

            _logger.Debug("Search for game [{0}] returned nothing ({1} in a row), next automatic search backed off by {2}", gameId, count, BackoffFor(count));
        }

        private static TimeSpan BackoffFor(int consecutiveEmpty)
        {
            if (consecutiveEmpty < 1)
            {
                return TimeSpan.Zero;
            }

            // Shift rather than Math.Pow, and cap the shift itself: 2^31 hours
            // overflows long before the 24h cap would have applied.
            if (consecutiveEmpty > 16)
            {
                return MaxBackoff;
            }

            var hours = TimeSpan.FromHours(1L << (consecutiveEmpty - 1));

            return hours > MaxBackoff ? MaxBackoff : hours;
        }
    }
}
