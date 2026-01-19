using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.Pending.PendingReleaseServiceTests
{
    [TestFixture]
    public class RemoveGrabbedFixture : CoreTest<PendingReleaseService>
    {
        private DownloadDecision _temporarilyRejected;
        private Game _game;
        private QualityProfile _profile;
        private ReleaseInfo _release;
        private ParsedGameInfo _parsedGameInfo;
        private RemoteGame _remoteGame;
        private List<PendingRelease> _heldReleases;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .Build();

            _profile = new QualityProfile
            {
                Name = "Test",
                Cutoff = Quality.Uplay.Id,
                Items = new List<QualityProfileQualityItem>
                                   {
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.Uplay },
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.Epic },
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.Repack }
                                   },
            };

            _game.QualityProfile = _profile;

            _release = Builder<ReleaseInfo>.CreateNew().Build();

            _parsedGameInfo = Builder<ParsedGameInfo>.CreateNew().Build();
            _parsedGameInfo.Quality = new QualityModel(Quality.Uplay);

            _remoteGame = new RemoteGame();
            _remoteGame.Game = _game;
            _remoteGame.ParsedGameInfo = _parsedGameInfo;
            _remoteGame.Release = _release;

            _temporarilyRejected = new DownloadDecision(_remoteGame, new DownloadRejection(DownloadRejectionReason.MinimumAgeDelay, "Temp Rejected", RejectionType.Temporary));

            _heldReleases = new List<PendingRelease>();

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(_heldReleases);

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.AllByGameId(It.IsAny<int>()))
                  .Returns<int>(i => _heldReleases.Where(v => v.GameId == i).ToList());

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(It.IsAny<int>()))
                  .Returns(_game);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGames(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Game> { _game });

            Mocker.GetMock<IPrioritizeDownloadDecision>()
                  .Setup(s => s.PrioritizeDecisionsForGames(It.IsAny<List<DownloadDecision>>()))
                  .Returns((List<DownloadDecision> d) => d);
        }

        private void GivenHeldRelease(QualityModel quality)
        {
            var parsedGameInfo = _parsedGameInfo.JsonClone();
            parsedGameInfo.Quality = quality;

            var heldReleases = Builder<PendingRelease>.CreateListOfSize(1)
                                                   .All()
                                                   .With(h => h.GameId = _game.Id)
                                                   .With(h => h.Release = _release.JsonClone())
                                                   .With(h => h.ParsedGameInfo = parsedGameInfo)
                                                   .Build();

            _heldReleases.AddRange(heldReleases);
        }

        [Test]
        public void should_delete_if_the_grabbed_quality_is_the_same()
        {
            GivenHeldRelease(_parsedGameInfo.Quality);

            Subject.Handle(new GameGrabbedEvent(_remoteGame));

            VerifyDelete();
        }

        [Test]
        public void should_delete_if_the_grabbed_quality_is_the_higher()
        {
            GivenHeldRelease(new QualityModel(Quality.Scene));

            Subject.Handle(new GameGrabbedEvent(_remoteGame));

            VerifyDelete();
        }

        [Test]
        public void should_not_delete_if_the_grabbed_quality_is_the_lower()
        {
            GivenHeldRelease(new QualityModel(Quality.Repack));

            Subject.Handle(new GameGrabbedEvent(_remoteGame));

            VerifyNoDelete();
        }

        private void VerifyDelete()
        {
            Mocker.GetMock<IPendingReleaseRepository>()
                .Verify(v => v.Delete(It.IsAny<PendingRelease>()), Times.Once());
        }

        private void VerifyNoDelete()
        {
            Mocker.GetMock<IPendingReleaseRepository>()
                .Verify(v => v.Delete(It.IsAny<PendingRelease>()), Times.Never());
        }
    }
}
