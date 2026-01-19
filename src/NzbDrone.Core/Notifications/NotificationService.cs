using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Update.History.Events;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<GameRenamedEvent>,
          IHandle<GameGrabbedEvent>,
          IHandle<GameFileImportedEvent>,
          IHandle<GamesDeletedEvent>,
          IHandle<GameAddedEvent>,
          IHandle<GamesImportedEvent>,
          IHandle<GameFileDeletedEvent>,
          IHandle<HealthCheckFailedEvent>,
          IHandle<HealthCheckRestoredEvent>,
          IHandle<UpdateInstalledEvent>,
          IHandle<ManualInteractionRequiredEvent>,
          IHandleAsync<DeleteCompletedEvent>,
          IHandleAsync<DownloadsProcessedEvent>,
          IHandleAsync<RenameCompletedEvent>,
          IHandleAsync<HealthCheckCompleteEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly INotificationStatusService _notificationStatusService;
        private readonly Logger _logger;

        public NotificationService(INotificationFactory notificationFactory, INotificationStatusService notificationStatusService, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _notificationStatusService = notificationStatusService;
            _logger = logger;
        }

        private string GetMessage(Game game, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            // IGDB - Internet Game Database (primary metadata source for games)
            var igdbUrl = "https://www.igdb.com/games/" + game.GameMetadata.Value.IgdbId;

            if (quality.Revision.Version > 1)
            {
                qualityString += " Proper";
            }

            return string.Format("{0} ({1}) [{2}] {3}",
                                    game.Title,
                                    game.Year,
                                    qualityString,
                                    igdbUrl);
        }

        private bool ShouldHandleGame(ProviderDefinition definition, Game game)
        {
            if (definition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");
                return true;
            }

            if (definition.Tags.Intersect(game.Tags).Any())
            {
                _logger.Debug("Notification and game have one or more intersecting tags.");
                return true;
            }

            // TODO: this message could be more clear
            _logger.Debug("{0} does not have any intersecting tags with {1}. Notification will not be sent", definition.Name, game.Title);
            return false;
        }

        private bool ShouldHandleHealthFailure(HealthCheck.HealthCheck healthCheck, bool includeWarnings)
        {
            if (healthCheck.Type == HealthCheckResult.Error)
            {
                return true;
            }

            if (healthCheck.Type == HealthCheckResult.Warning && includeWarnings)
            {
                return true;
            }

            return false;
        }

        public void Handle(GameGrabbedEvent message)
        {
            var grabMessage = new GrabMessage
            {
                Message = GetMessage(message.Game.Game, message.Game.ParsedGameInfo.Quality),
                Quality = message.Game.ParsedGameInfo.Quality,
                Game = message.Game.Game,
                RemoteGame = message.Game,
                DownloadClientType = message.DownloadClient,
                DownloadClientName = message.DownloadClientName,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleGame(notification.Definition, message.Game.Game))
                    {
                        continue;
                    }

                    notification.OnGrab(grabMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Error(ex, "Unable to send OnGrab notification to {0}", notification.Definition.Name);
                }
            }
        }

        public void Handle(GameFileImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadMessage = new DownloadMessage
            {
                Message = GetMessage(message.GameInfo.Game, message.GameInfo.Quality),
                GameInfo = message.GameInfo,
                GameFile = message.ImportedGame,
                Game = message.GameInfo.Game,
                OldGameFiles = message.OldFiles,
                SourcePath = message.GameInfo.Path,
                DownloadClientInfo = message.DownloadClientInfo,
                DownloadId = message.DownloadId,
                Release = message.GameInfo.Release
            };

            foreach (var notification in _notificationFactory.OnDownloadEnabled())
            {
                try
                {
                    if (ShouldHandleGame(notification.Definition, message.GameInfo.Game))
                    {
                        if (downloadMessage.OldGameFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
                        {
                            notification.OnDownload(downloadMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnDownload notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(GameAddedEvent message)
        {
            foreach (var notification in _notificationFactory.OnGameAddedEnabled())
            {
                try
                {
                    if (ShouldHandleGame(notification.Definition, message.Game))
                    {
                        notification.OnGameAdded(message.Game);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnGameAdded notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(GamesImportedEvent message)
        {
            foreach (var notification in _notificationFactory.OnGameAddedEnabled())
            {
                try
                {
                    foreach (var game in message.Games)
                    {
                        if (ShouldHandleGame(notification.Definition, game))
                        {
                            notification.OnGameAdded(game);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnGameAdded notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(GameRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleGame(notification.Definition, message.Game))
                    {
                        notification.OnGameRename(message.Game, message.RenamedFiles);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnRename notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(UpdateInstalledEvent message)
        {
            var updateMessage = new ApplicationUpdateMessage();
            updateMessage.Message = $"Gamarr updated from {message.PreviousVerison.ToString()} to {message.NewVersion.ToString()}";
            updateMessage.PreviousVersion = message.PreviousVerison;
            updateMessage.NewVersion = message.NewVersion;

            foreach (var notification in _notificationFactory.OnApplicationUpdateEnabled())
            {
                try
                {
                    notification.OnApplicationUpdate(updateMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnApplicationUpdate notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(ManualInteractionRequiredEvent message)
        {
            var game = message.RemoteGame?.Game;
            var mess = "";

            if (game != null)
            {
                mess = GetMessage(game, message.RemoteGame.ParsedGameInfo.Quality);
            }

            if (mess.IsNullOrWhiteSpace() && message.TrackedDownload.DownloadItem != null)
            {
                mess = message.TrackedDownload.DownloadItem.Title;
            }

            if (mess.IsNullOrWhiteSpace())
            {
                return;
            }

            var manualInteractionMessage = new ManualInteractionRequiredMessage
            {
                Message = mess,
                Game = game,
                Quality = message.RemoteGame?.ParsedGameInfo.Quality,
                RemoteGame = message.RemoteGame,
                TrackedDownload = message.TrackedDownload,
                DownloadClientInfo = message.TrackedDownload.DownloadItem?.DownloadClientInfo,
                DownloadId = message.TrackedDownload.DownloadItem?.DownloadId,
                Release = message.Release
            };

            foreach (var notification in _notificationFactory.OnManualInteractionEnabled())
            {
                try
                {
                    if (!ShouldHandleGame(notification.Definition, message.RemoteGame.Game))
                    {
                        continue;
                    }

                    notification.OnManualInteractionRequired(manualInteractionMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Error(ex, "Unable to send OnManualInteractionRequired notification to {0}", notification.Definition.Name);
                }
            }
        }

        public void Handle(GameFileDeletedEvent message)
        {
            var deleteMessage = new GameFileDeleteMessage();
            deleteMessage.Message = GetMessage(message.GameFile.Game, message.GameFile.Quality);
            deleteMessage.GameFile = message.GameFile;
            deleteMessage.Game = message.GameFile.Game;
            deleteMessage.Reason = message.Reason;

            foreach (var notification in _notificationFactory.OnGameFileDeleteEnabled())
            {
                try
                {
                    if (message.Reason != MediaFiles.DeleteMediaFileReason.Upgrade || ((NotificationDefinition)notification.Definition).OnGameFileDeleteForUpgrade)
                    {
                        if (ShouldHandleGame(notification.Definition, message.GameFile.Game))
                        {
                            notification.OnGameFileDelete(deleteMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnGameFileDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(GamesDeletedEvent message)
        {
            foreach (var game in message.Games)
            {
                var deleteMessage = new GameDeleteMessage(game, message.DeleteFiles);

                foreach (var notification in _notificationFactory.OnGameDeleteEnabled())
                {
                    try
                    {
                        if (ShouldHandleGame(notification.Definition, deleteMessage.Game))
                        {
                            notification.OnGameDelete(deleteMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _notificationStatusService.RecordFailure(notification.Definition.Id);
                        _logger.Warn(ex, "Unable to send OnGameDelete notification to: " + notification.Definition.Name);
                    }
                }
            }
        }

        public void Handle(HealthCheckFailedEvent message)
        {
            // Don't send health check notifications during the start up grace period,
            // once that duration expires they they'll be retested and fired off if necessary.
            if (message.IsInStartupGracePeriod)
            {
                return;
            }

            foreach (var notification in _notificationFactory.OnHealthIssueEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.HealthCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthIssue(message.HealthCheck);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnHealthIssue notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(HealthCheckRestoredEvent message)
        {
            if (message.IsInStartupGracePeriod)
            {
                return;
            }

            foreach (var notification in _notificationFactory.OnHealthRestoredEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.PreviousCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthRestored(message.PreviousCheck);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnHealthRestored notification to: " + notification.Definition.Name);
                }
            }
        }

        public void HandleAsync(DeleteCompletedEvent message)
        {
            ProcessQueue();
        }

        public void HandleAsync(DownloadsProcessedEvent message)
        {
            ProcessQueue();
        }

        public void HandleAsync(RenameCompletedEvent message)
        {
            ProcessQueue();
        }

        public void HandleAsync(HealthCheckCompleteEvent message)
        {
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            foreach (var notification in _notificationFactory.GetAvailableProviders())
            {
                try
                {
                    notification.ProcessQueue();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to process notification queue for " + notification.Definition.Name);
                }
            }
        }
    }
}
