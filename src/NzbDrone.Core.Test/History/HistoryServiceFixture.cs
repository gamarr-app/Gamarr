using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.History
{
    [TestFixture]
    public class HistoryServiceFixture : CoreTest<HistoryService>
    {
        private GameHistory _history;

        [SetUp]
        public void Setup()
        {
            _history = new GameHistory
            {
                Id = 1,
                GameId = 1,
                SourceTitle = "Test Game 2023",
                Quality = new QualityModel(Quality.GOG),
                Date = DateTime.UtcNow,
                EventType = GameHistoryEventType.Grabbed
            };

            Mocker.GetMock<IHistoryRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<GameHistory> { _history });
        }

        [Test]
        public void should_get_history_by_game_id()
        {
            Mocker.GetMock<IHistoryRepository>()
                  .Setup(s => s.FindByGameId(1))
                  .Returns(new List<GameHistory> { _history });

            var result = Subject.GetByGameId(1);

            result.Should().HaveCount(1);
            result[0].GameId.Should().Be(1);
        }

        [Test]
        public void should_get_recent_history()
        {
            Mocker.GetMock<IHistoryRepository>()
                  .Setup(s => s.Since(It.IsAny<DateTime>(), It.IsAny<GameHistoryEventType?>()))
                  .Returns(new List<GameHistory> { _history });

            var result = Subject.Since(DateTime.UtcNow.AddDays(-7), null);

            result.Should().HaveCount(1);
        }

        [Test]
        public void should_purge_history()
        {
            Subject.Purge();

            Mocker.GetMock<IHistoryRepository>()
                  .Verify(s => s.Purge(true), Times.Once());
        }

        [Test]
        public void should_trim_history()
        {
            Subject.Trim();

            Mocker.GetMock<IHistoryRepository>()
                  .Verify(s => s.Trim(), Times.Once());
        }

        [Test]
        public void should_get_last_grabbed_history()
        {
            Mocker.GetMock<IHistoryRepository>()
                  .Setup(s => s.MostRecentForGame(1))
                  .Returns(_history);

            var result = Subject.MostRecentForGame(1);

            result.Should().Be(_history);
        }

        [Test]
        public void should_find_by_download_id()
        {
            Mocker.GetMock<IHistoryRepository>()
                  .Setup(s => s.FindByDownloadId("abc123"))
                  .Returns(new List<GameHistory> { _history });

            var result = Subject.FindByDownloadId("abc123");

            result.Should().HaveCount(1);
        }
    }
}
