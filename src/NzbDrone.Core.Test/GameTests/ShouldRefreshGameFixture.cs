using System;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class ShouldRefreshGameFixture : TestBase<ShouldRefreshGame>
    {
        private GameMetadata _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<GameMetadata>.CreateNew()
                                     .With(v => v.Status = GameStatusType.InDevelopment)
                                     .With(m => m.PhysicalRelease = DateTime.Today.AddDays(-100))
                                     .Build();
        }

        private void GivenGameIsAnnouced()
        {
            _game.Status = GameStatusType.Announced;
        }

        private void GivenGameIsReleased()
        {
            _game.Status = GameStatusType.Released;
        }

        private void GivenGameLastRefreshedMonthsAgo()
        {
            _game.LastInfoSync = DateTime.UtcNow.AddDays(-190);
        }

        private void GivenGameLastRefreshedYesterday()
        {
            _game.LastInfoSync = DateTime.UtcNow.AddDays(-1);
        }

        private void GivenGameLastRefreshedADayAgo()
        {
            _game.LastInfoSync = DateTime.UtcNow.AddHours(-24);
        }

        private void GivenGameLastRefreshedRecently()
        {
            _game.LastInfoSync = DateTime.UtcNow.AddHours(-1);
        }

        private void GivenRecentlyReleased()
        {
            _game.PhysicalRelease = DateTime.Today.AddDays(-7);
        }

        [Test]
        public void should_return_true_if_in_cinemas_game_last_refreshed_more_than_12_hours_ago()
        {
            GivenGameLastRefreshedADayAgo();

            Subject.ShouldRefresh(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_in_cinemas_game_last_refreshed_less_than_12_hours_ago()
        {
            GivenGameLastRefreshedRecently();

            Subject.ShouldRefresh(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_released_game_last_refreshed_yesterday()
        {
            GivenGameIsReleased();
            GivenGameLastRefreshedYesterday();

            Subject.ShouldRefresh(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_game_last_refreshed_more_than_30_days_ago()
        {
            GivenGameIsReleased();
            GivenGameLastRefreshedMonthsAgo();

            Subject.ShouldRefresh(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_episode_aired_in_last_30_days()
        {
            GivenGameIsReleased();
            GivenGameLastRefreshedYesterday();

            GivenRecentlyReleased();

            Subject.ShouldRefresh(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_recently_refreshed_released_game_released_30_days()
        {
            GivenGameIsReleased();
            GivenGameLastRefreshedYesterday();

            Subject.ShouldRefresh(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_recently_refreshed_ended_show_aired_in_last_30_days()
        {
            GivenGameIsReleased();
            GivenGameLastRefreshedRecently();

            GivenRecentlyReleased();

            Subject.ShouldRefresh(_game).Should().BeFalse();
        }
    }
}
