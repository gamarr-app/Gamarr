using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class QueueSpecificationFixture : CoreTest<QueueSpecification>
    {
        private Game _game;
        private RemoteGame _remoteGame;

        private Game _otherGame;

        private ReleaseInfo _releaseInfo;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            CustomFormatsTestHelpers.GivenCustomFormats();

            _game = Builder<Game>.CreateNew()
                                     .With(e => e.QualityProfile = new QualityProfile
                                     {
                                         Items = Qualities.QualityFixture.GetDefaultQualities(),
                                         FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(),
                                         MinFormatScore = 0,
                                         UpgradeAllowed = true
                                     })
                                     .Build();

            _otherGame = Builder<Game>.CreateNew()
                                          .With(s => s.Id = 2)
                                          .Build();

            _releaseInfo = Builder<ReleaseInfo>.CreateNew()
                                   .Build();

            _remoteGame = Builder<RemoteGame>.CreateNew()
                .With(r => r.Game = _game)
                .With(r => r.ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.Scene) })
                .With(x => x.CustomFormats = new List<CustomFormat>())
                .Build();

            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(x => x.ParseCustomFormat(It.IsAny<RemoteGame>(), It.IsAny<long>()))
                .Returns(new List<CustomFormat>());
        }

        private void GivenEmptyQueue()
        {
            Mocker.GetMock<IQueueService>()
                .Setup(s => s.GetQueue())
                .Returns(new List<Queue.Queue>());
        }

        private void GivenQueue(IEnumerable<RemoteGame> remoteGames, TrackedDownloadState trackedDownloadState = TrackedDownloadState.Downloading)
        {
            var queue = remoteGames.Select(remoteGame => new Queue.Queue
            {
                RemoteGame = remoteGame,
                TrackedDownloadState = trackedDownloadState
            });

            Mocker.GetMock<IQueueService>()
                .Setup(s => s.GetQueue())
                .Returns(queue.ToList());
        }

        [Test]
        public void should_return_true_when_queue_is_empty()
        {
            GivenEmptyQueue();
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_game_doesnt_match()
        {
            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                       .With(r => r.Game = _otherGame)
                                                       .Build();

            GivenQueue(new List<RemoteGame> { remoteGame });
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_quality_in_queue_is_lower()
        {
            _game.QualityProfile.Cutoff = Quality.Steam.Id;

            // Set _remoteGame to have higher quality (GOG)
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG);

            // Queue item has lower quality (Scene)
            var remoteGame = Builder<RemoteGame>.CreateNew()
                .With(r => r.Game = _game)
                .With(r => r.ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Scene)
                })
                .With(x => x.CustomFormats = new List<CustomFormat>())
                .Build();

            GivenQueue(new List<RemoteGame> { remoteGame });
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_qualities_are_the_same()
        {
            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(r => r.Game = _game)
                                                      .With(r => r.ParsedGameInfo = new ParsedGameInfo
                                                      {
                                                          Quality = new QualityModel(Quality.Scene)
                                                      })
                                                      .Build();

            GivenQueue(new List<RemoteGame> { remoteGame });
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_quality_in_queue_is_better()
        {
            _game.QualityProfile.Cutoff = Quality.GOG.Id;

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(r => r.Game = _game)
                                                      .With(r => r.ParsedGameInfo = new ParsedGameInfo
                                                      {
                                                          Quality = new QualityModel(Quality.Uplay)
                                                      })
                                                      .Build();

            GivenQueue(new List<RemoteGame> { remoteGame });
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_quality_in_queue_meets_cutoff()
        {
            _game.QualityProfile.Cutoff = _remoteGame.ParsedGameInfo.Quality.Quality.Id;

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(r => r.Game = _game)
                                                      .With(r => r.ParsedGameInfo = new ParsedGameInfo
                                                      {
                                                          Quality = new QualityModel(Quality.Uplay)
                                                      })
                                                      .Build();

            GivenQueue(new List<RemoteGame> { remoteGame });

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_quality_is_better_and_upgrade_allowed_is_false_for_quality_profile()
        {
            _game.QualityProfile.Cutoff = Quality.GOG.Id;
            _game.QualityProfile.UpgradeAllowed = false;

            var remoteGame = Builder<RemoteGame>.CreateNew()
                .With(r => r.Game = _game)
                .With(r => r.ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.GOG)
                })
                .Build();

            GivenQueue(new List<RemoteGame> { remoteGame });
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_everything_is_the_same_for_failed_pending()
        {
            _game.QualityProfile.Cutoff = Quality.GOG.Id;

            var remoteGame = Builder<RemoteGame>.CreateNew()
                .With(r => r.Game = _game)
                .With(r => r.ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Scene)
                })
                .With(r => r.Release = _releaseInfo)
                .Build();

            GivenQueue(new List<RemoteGame> { remoteGame }, TrackedDownloadState.FailedPending);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_same_quality_non_proper_in_queue_and_download_propers_is_do_not_upgrade()
        {
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Uplay, new Revision(2));
            _game.QualityProfile.Cutoff = _remoteGame.ParsedGameInfo.Quality.Quality.Id;

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.DownloadPropersAndRepacks)
                .Returns(ProperDownloadTypes.DoNotUpgrade);

            var remoteGame = Builder<RemoteGame>.CreateNew()
                .With(r => r.Game = _game)
                .With(r => r.ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Uplay),
                    Languages = new List<Language> { Language.English }
                })
                .With(r => r.Release = _releaseInfo)
                .With(r => r.CustomFormats = new List<CustomFormat>())
                .Build();

            GivenQueue(new List<RemoteGame> { remoteGame });

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }
    }
}
