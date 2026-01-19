using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Specifications
{
    [TestFixture]
    public class GrabbedReleaseQualityFixture : CoreTest<GrabbedReleaseQualitySpecification>
    {
        private LocalGame _localGame;
        private DownloadClientItem _downloadClientItem;

        [SetUp]
        public void Setup()
        {
            _localGame = Builder<LocalGame>.CreateNew()
                                                 .With(l => l.Quality = new QualityModel(Quality.Repack))
                                                 .Build();

            _downloadClientItem = Builder<DownloadClientItem>.CreateNew()
                                                             .Build();
        }

        private void GivenHistory(List<GameHistory> history)
        {
            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(history);
        }

        [Test]
        public void should_be_accepted_when_downloadClientItem_is_null()
        {
            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_no_history_for_downloadId()
        {
            GivenHistory(new List<GameHistory>());

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_no_grabbed_history_for_downloadId()
        {
            var history = Builder<GameHistory>.CreateListOfSize(1)
                                                  .All()
                                                  .With(h => h.EventType = GameHistoryEventType.Unknown)
                                                  .BuildList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_grabbed_history_quality_is_unknown()
        {
            var history = Builder<GameHistory>.CreateListOfSize(1)
                                                  .All()
                                                  .With(h => h.EventType = GameHistoryEventType.Grabbed)
                                                  .With(h => h.Quality = new QualityModel(Quality.Unknown))
                                                  .BuildList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_grabbed_history_quality_matches()
        {
            var history = Builder<GameHistory>.CreateListOfSize(1)
                                                  .All()
                                                  .With(h => h.EventType = GameHistoryEventType.Grabbed)
                                                  .With(h => h.Quality = _localGame.Quality)
                                                  .BuildList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_grabbed_history_quality_does_not_match_log_only()
        {
            var history = Builder<GameHistory>.CreateListOfSize(1)
                                                  .All()
                                                  .With(h => h.EventType = GameHistoryEventType.Grabbed)
                                                  .With(h => h.Quality = new QualityModel(Quality.Uplay))
                                                  .BuildList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }
    }
}
