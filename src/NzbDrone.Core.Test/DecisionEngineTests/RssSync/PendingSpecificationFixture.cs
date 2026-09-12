using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Games;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class PendingSpecificationFixture : CoreTest<PendingSpecification>
    {
        private Game _game;
        private RemoteGame _remoteGame;

        private Game _otherGame;

        private ReleaseDecisionInformation _information = new ReleaseDecisionInformation(false, null);

        [SetUp]
        public void Setup()
        {
            CustomFormatsTestHelpers.GivenCustomFormats();

            _game = Builder<Game>.CreateNew()
                .With(e => e.QualityProfile = new QualityProfile
                {
                    UpgradeAllowed = true,
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(),
                    MinFormatScore = 0
                })
                .Build();

            _otherGame = Builder<Game>.CreateNew()
                .With(s => s.Id = 2)
                .Build();

            _remoteGame = Builder<RemoteGame>.CreateNew()
                .With(r => r.Game = _game)
                .With(r => r.ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.Steam), Languages = new List<Language> { Language.Spanish } })
                .With(r => r.CustomFormats = new List<CustomFormat>())
                .Build();

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(x => x.ParseCustomFormat(It.IsAny<RemoteGame>(), It.IsAny<long>()))
                  .Returns(new List<CustomFormat>());
        }

        private void GivenEmptyPendingQueue()
        {
            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(new List<NzbDrone.Core.Queue.Queue>());
        }

        private void GivenPendingQueue(IEnumerable<RemoteGame> remoteGames)
        {
            var queue = remoteGames.Select(remoteGame => new NzbDrone.Core.Queue.Queue
            {
                RemoteGame = remoteGame
            });

            Mocker.GetMock<IPendingReleaseService>()
                .Setup(s => s.GetPendingQueue())
                .Returns(queue.ToList());
        }

        [Test]
        public void should_return_true_when_pending_queue_is_empty()
        {
            GivenEmptyPendingQueue();

            Subject.IsSatisfiedBy(_remoteGame, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

        [Test]
        public void should_return_true_when_not_pushed_release()
        {
            _remoteGame.ReleaseSource = ReleaseSourceType.Rss;

            GivenEmptyPendingQueue();

            Subject.IsSatisfiedBy(_remoteGame, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Never);
        }

        [Test]
        public void should_return_true_when_game_is_not_pending()
        {
            GivenEmptyPendingQueue();

            GivenPendingQueue(new List<RemoteGame>
            {
                new RemoteGame
                {
                    Game = _otherGame,
                }
            });

            Subject.IsSatisfiedBy(_remoteGame, _information).Accepted.Should().BeTrue();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }

        [Test]
        public void should_return_false_when_game_is_pending()
        {
            GivenEmptyPendingQueue();

            GivenPendingQueue(new List<RemoteGame>
            {
                new RemoteGame
                {
                    Game = _game,
                }
            });

            Subject.IsSatisfiedBy(_remoteGame, _information).Accepted.Should().BeFalse();

            Mocker.GetMock<IPendingReleaseService>()
                .Verify(s => s.GetPendingQueue(), Times.Once);
        }
    }
}
