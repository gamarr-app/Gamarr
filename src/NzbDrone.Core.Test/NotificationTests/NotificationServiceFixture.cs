using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class NotificationServiceFixture : CoreTest<NotificationService>
    {
        private Mock<INotification> _notification;

        [SetUp]
        public void Setup()
        {
            _notification = new Mock<INotification>();
            _notification.Setup(n => n.Definition)
                .Returns(new NotificationDefinition
                {
                    Id = 1,
                    Name = "TestNotification",
                    OnGrab = true,
                    OnDownload = true,
                    OnUpgrade = true,
                    OnRename = true,
                    OnGameAdded = true,
                    OnGameDelete = true,
                    OnGameFileDelete = true,
                    OnGameFileDeleteForUpgrade = true,
                    OnHealthIssue = true,
                    IncludeHealthWarnings = true,
                    OnHealthRestored = true,
                    OnApplicationUpdate = true,
                    OnManualInteractionRequired = true,
                    Tags = new HashSet<int>()
                });
        }

        private Game GetGame()
        {
            return new Game
            {
                Id = 1,
                Title = "Test Game",
                Year = 2024,
                Tags = new HashSet<int>(),
                GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                {
                    IgdbSlug = "test-game"
                })
            };
        }

        [Test]
        public void should_send_grab_notification()
        {
            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnGrabEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var game = GetGame();
            var remoteGame = new RemoteGame
            {
                Game = game,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Unknown)
                }
            };

            Subject.Handle(new GameGrabbedEvent(remoteGame));

            _notification.Verify(n => n.OnGrab(It.IsAny<GrabMessage>()), Times.Once());
        }

        [Test]
        public void should_not_send_grab_notification_when_tags_dont_match()
        {
            _notification.Setup(n => n.Definition)
                .Returns(new NotificationDefinition
                {
                    Id = 1,
                    Name = "TestNotification",
                    OnGrab = true,
                    Tags = new HashSet<int> { 99 }
                });

            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnGrabEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var game = GetGame();
            game.Tags = new HashSet<int> { 1 };

            var remoteGame = new RemoteGame
            {
                Game = game,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Unknown)
                }
            };

            Subject.Handle(new GameGrabbedEvent(remoteGame));

            _notification.Verify(n => n.OnGrab(It.IsAny<GrabMessage>()), Times.Never());
        }

        [Test]
        public void should_send_download_notification_for_new_download()
        {
            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnDownloadEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var game = GetGame();
            var gameInfo = new LocalGame
            {
                Game = game,
                Quality = new QualityModel(Quality.Unknown),
                Path = "/test/path"
            };

            Subject.Handle(new GameFileImportedEvent(
                gameInfo,
                new GameFile(),
                new List<DeletedGameFile>(),
                true,
                null));

            _notification.Verify(n => n.OnDownload(It.IsAny<DownloadMessage>()), Times.Once());
        }

        [Test]
        public void should_not_send_download_notification_for_existing_file()
        {
            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnDownloadEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var game = GetGame();
            var gameInfo = new LocalGame
            {
                Game = game,
                Quality = new QualityModel(Quality.Unknown),
                Path = "/test/path"
            };

            Subject.Handle(new GameFileImportedEvent(
                gameInfo,
                new GameFile(),
                new List<DeletedGameFile>(),
                false,
                null));

            _notification.Verify(n => n.OnDownload(It.IsAny<DownloadMessage>()), Times.Never());
        }

        [Test]
        public void should_not_send_upgrade_notification_when_upgrade_disabled()
        {
            _notification.Setup(n => n.Definition)
                .Returns(new NotificationDefinition
                {
                    Id = 1,
                    Name = "TestNotification",
                    OnDownload = true,
                    OnUpgrade = false,
                    Tags = new HashSet<int>()
                });

            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnDownloadEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var game = GetGame();
            var gameInfo = new LocalGame
            {
                Game = game,
                Quality = new QualityModel(Quality.Unknown),
                Path = "/test/path"
            };

            Subject.Handle(new GameFileImportedEvent(
                gameInfo,
                new GameFile(),
                new List<DeletedGameFile> { new DeletedGameFile(new GameFile(), null) },
                true,
                null));

            _notification.Verify(n => n.OnDownload(It.IsAny<DownloadMessage>()), Times.Never());
        }

        [Test]
        public void should_send_game_added_notification()
        {
            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnGameAddedEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            Subject.Handle(new GameAddedEvent(GetGame()));

            _notification.Verify(n => n.OnGameAdded(It.IsAny<Game>()), Times.Once());
        }

        [Test]
        public void should_send_game_deleted_notification()
        {
            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnGameDeleteEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            Subject.Handle(new GamesDeletedEvent(new List<Game> { GetGame() }, true, true));

            _notification.Verify(n => n.OnGameDelete(It.IsAny<GameDeleteMessage>()), Times.Once());
        }

        [Test]
        public void should_send_health_check_notification_for_error()
        {
            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnHealthIssueEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var healthCheck = new NzbDrone.Core.HealthCheck.HealthCheck(typeof(NotificationServiceFixture), NzbDrone.Core.HealthCheck.HealthCheckResult.Error, "Test error");

            Subject.Handle(new NzbDrone.Core.HealthCheck.HealthCheckFailedEvent(healthCheck, false));

            _notification.Verify(n => n.OnHealthIssue(healthCheck), Times.Once());
        }

        [Test]
        public void should_not_send_health_warning_when_not_configured()
        {
            _notification.Setup(n => n.Definition)
                .Returns(new NotificationDefinition
                {
                    Id = 1,
                    Name = "TestNotification",
                    OnHealthIssue = true,
                    IncludeHealthWarnings = false,
                    Tags = new HashSet<int>()
                });

            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnHealthIssueEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var healthCheck = new NzbDrone.Core.HealthCheck.HealthCheck(typeof(NotificationServiceFixture), NzbDrone.Core.HealthCheck.HealthCheckResult.Warning, "Test warning");

            Subject.Handle(new NzbDrone.Core.HealthCheck.HealthCheckFailedEvent(healthCheck, false));

            _notification.Verify(n => n.OnHealthIssue(It.IsAny<NzbDrone.Core.HealthCheck.HealthCheck>()), Times.Never());
        }

        [Test]
        public void should_not_send_health_notification_during_startup_grace()
        {
            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnHealthIssueEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var healthCheck = new NzbDrone.Core.HealthCheck.HealthCheck(typeof(NotificationServiceFixture), NzbDrone.Core.HealthCheck.HealthCheckResult.Error, "Test error");

            Subject.Handle(new NzbDrone.Core.HealthCheck.HealthCheckFailedEvent(healthCheck, true));

            _notification.Verify(n => n.OnHealthIssue(It.IsAny<NzbDrone.Core.HealthCheck.HealthCheck>()), Times.Never());
        }

        [Test]
        public void should_record_failure_when_notification_throws()
        {
            _notification.Setup(n => n.OnGrab(It.IsAny<GrabMessage>()))
                .Throws(new Exception("notification failed"));

            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnGrabEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var game = GetGame();
            var remoteGame = new RemoteGame
            {
                Game = game,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Unknown)
                }
            };

            Subject.Handle(new GameGrabbedEvent(remoteGame));

            Mocker.GetMock<INotificationStatusService>()
                .Verify(s => s.RecordFailure(1, It.IsAny<TimeSpan>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_record_success_on_successful_notification()
        {
            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnGrabEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object });

            var game = GetGame();
            var remoteGame = new RemoteGame
            {
                Game = game,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Unknown)
                }
            };

            Subject.Handle(new GameGrabbedEvent(remoteGame));

            Mocker.GetMock<INotificationStatusService>()
                .Verify(s => s.RecordSuccess(1), Times.Once());
        }

        [Test]
        public void should_send_to_multiple_notifications()
        {
            var notification2 = new Mock<INotification>();
            notification2.Setup(n => n.Definition)
                .Returns(new NotificationDefinition
                {
                    Id = 2,
                    Name = "TestNotification2",
                    OnGrab = true,
                    Tags = new HashSet<int>()
                });

            Mocker.GetMock<INotificationFactory>()
                .Setup(f => f.OnGrabEnabled(It.IsAny<bool>()))
                .Returns(new List<INotification> { _notification.Object, notification2.Object });

            var game = GetGame();
            var remoteGame = new RemoteGame
            {
                Game = game,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Unknown)
                }
            };

            Subject.Handle(new GameGrabbedEvent(remoteGame));

            _notification.Verify(n => n.OnGrab(It.IsAny<GrabMessage>()), Times.Once());
            notification2.Verify(n => n.OnGrab(It.IsAny<GrabMessage>()), Times.Once());
        }
    }
}
