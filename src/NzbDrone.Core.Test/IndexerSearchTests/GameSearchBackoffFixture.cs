using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Games;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class GameSearchBackoffFixture : CoreTest<GameSearchBackoff>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());
        }

        private static Game GameSearchedMinutesAgo(int id, double minutes)
        {
            return new Game
            {
                Id = id,
                Title = "Lords of Thunder",
                LastSearchTime = DateTime.UtcNow.AddMinutes(-minutes)
            };
        }

        [Test]
        public void should_not_skip_a_game_that_has_never_been_searched()
        {
            Subject.ShouldSkip(new Game { Id = 57, LastSearchTime = null }).Should().BeFalse();
        }

        [Test]
        public void should_not_skip_before_any_empty_search_is_recorded()
        {
            Subject.ShouldSkip(GameSearchedMinutesAgo(57, 1)).Should().BeFalse();
        }

        [Test]
        public void should_skip_within_the_first_hour_after_one_empty_search()
        {
            Subject.Record(57, 0);

            Subject.ShouldSkip(GameSearchedMinutesAgo(57, 30)).Should().BeTrue();
        }

        [Test]
        public void should_search_again_once_the_backoff_window_has_passed()
        {
            Subject.Record(57, 0);

            Subject.ShouldSkip(GameSearchedMinutesAgo(57, 61)).Should().BeFalse();
        }

        // This is the reported behaviour: ~30 minutes apart, indefinitely, for a
        // game no indexer carries. The second empty search must buy more than the
        // first, or the loop only slows down rather than stopping.
        [Test]
        public void should_escalate_the_window_with_each_consecutive_empty_search()
        {
            Subject.Record(57, 0);
            Subject.Record(57, 0);

            Subject.ShouldSkip(GameSearchedMinutesAgo(57, 90)).Should().BeTrue();
            Subject.ShouldSkip(GameSearchedMinutesAgo(57, 121)).Should().BeFalse();
        }

        [Test]
        public void should_cap_the_window_at_a_day_however_long_the_run_of_empties()
        {
            for (var i = 0; i < 40; i++)
            {
                Subject.Record(57, 0);
            }

            Subject.ShouldSkip(GameSearchedMinutesAgo(57, (23 * 60) + 59)).Should().BeTrue();
            Subject.ShouldSkip(GameSearchedMinutesAgo(57, (24 * 60) + 1)).Should().BeFalse();
        }

        // Any decision at all means an indexer answered about this game, so the
        // run is broken - including a decision we went on to reject.
        [Test]
        public void should_clear_the_backoff_when_a_search_returns_anything()
        {
            Subject.Record(57, 0);
            Subject.Record(57, 0);
            Subject.Record(57, 1);

            Subject.ShouldSkip(GameSearchedMinutesAgo(57, 1)).Should().BeFalse();
        }

        [Test]
        public void should_back_off_each_game_independently()
        {
            Subject.Record(57, 0);

            Subject.ShouldSkip(GameSearchedMinutesAgo(57, 30)).Should().BeTrue();
            Subject.ShouldSkip(GameSearchedMinutesAgo(58, 30)).Should().BeFalse();
        }
    }
}
