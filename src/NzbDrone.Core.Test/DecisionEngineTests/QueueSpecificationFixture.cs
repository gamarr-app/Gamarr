using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Games;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class QueueSpecificationFixture : CoreTest<QueueSpecification>
    {
        private RemoteGame _remoteGame;
        private Game _game;
        private QualityProfile _profile;

        [SetUp]
        public void Setup()
        {
            _profile = new QualityProfile
            {
                Cutoff = Quality.GOG.Id,
                Items = new List<QualityProfileQualityItem>
                {
                    new QualityProfileQualityItem { Quality = Quality.Unknown, Allowed = true },
                    new QualityProfileQualityItem { Quality = Quality.GOG, Allowed = true }
                },
                UpgradeAllowed = true
            };

            _game = new Game
            {
                Id = 1,
                QualityProfileId = 1,
                QualityProfile = _profile
            };

            _remoteGame = new RemoteGame
            {
                Game = _game,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.GOG)
                },
                Release = new ReleaseInfo
                {
                    Title = "Game.Title.2023",
                    IndexerId = 1,
                    DownloadProtocol = DownloadProtocol.Usenet
                }
            };

            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Returns(new List<NzbDrone.Core.Queue.Queue>());

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(s => s.ParseCustomFormat(It.IsAny<RemoteGame>(), It.IsAny<long>()))
                  .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_accept_when_queue_is_empty()
        {
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_no_matching_game_in_queue()
        {
            var queue = new List<NzbDrone.Core.Queue.Queue>
            {
                new NzbDrone.Core.Queue.Queue
                {
                    RemoteGame = new RemoteGame
                    {
                        Game = new Game { Id = 999 },
                        ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.Unknown) }
                    },
                    Size = 1000
                }
            };

            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Returns(queue);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_queue_item_is_failed_pending()
        {
            var queue = new List<NzbDrone.Core.Queue.Queue>
            {
                new NzbDrone.Core.Queue.Queue
                {
                    RemoteGame = new RemoteGame
                    {
                        Game = _game,
                        ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.GOG) }
                    },
                    Size = 1000,
                    TrackedDownloadState = TrackedDownloadState.FailedPending
                }
            };

            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Returns(queue);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_have_default_priority()
        {
            Subject.Priority.Should().Be(SpecificationPriority.Default);
        }

        [Test]
        public void should_have_permanent_rejection_type()
        {
            Subject.Type.Should().Be(RejectionType.Permanent);
        }
    }
}
