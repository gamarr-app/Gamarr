using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Emby
{
    public class MediaBrowser : NotificationBase<MediaBrowserSettings>
    {
        private readonly IMediaBrowserService _mediaBrowserService;
        private readonly MediaServerUpdateQueue<MediaBrowser, string> _updateQueue;
        private readonly Logger _logger;

        private static string Created = "Created";
        private static string Deleted = "Deleted";
        private static string Modified = "Modified";

        public MediaBrowser(IMediaBrowserService mediaBrowserService, ICacheManager cacheManager, Logger logger)
        {
            _mediaBrowserService = mediaBrowserService;
            _updateQueue = new MediaServerUpdateQueue<MediaBrowser, string>(cacheManager);
            _logger = logger;
        }

        public override string Link => "https://emby.media/";
        public override string Name => "Emby / Jellyfin";

        public override void OnGrab(GrabMessage grabMessage)
        {
            if (Settings.Notify)
            {
                _mediaBrowserService.Notify(Settings, GAME_GRABBED_TITLE_BRANDED, grabMessage.Message);
            }
        }

        public override void OnDownload(DownloadMessage message)
        {
            if (Settings.Notify)
            {
                _mediaBrowserService.Notify(Settings, GAME_DOWNLOADED_TITLE_BRANDED, message.Message);
            }

            UpdateIfEnabled(message.Game, Created);
        }

        public override void OnGameRename(Game game, List<RenamedGameFile> renamedFiles)
        {
            UpdateIfEnabled(game, Modified);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck message)
        {
            if (Settings.Notify)
            {
                _mediaBrowserService.Notify(Settings, HEALTH_ISSUE_TITLE_BRANDED, message.Message);
            }
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousMessage)
        {
            if (Settings.Notify)
            {
                _mediaBrowserService.Notify(Settings, HEALTH_RESTORED_TITLE_BRANDED, $"The following issue is now resolved: {previousMessage.Message}");
            }
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            if (Settings.Notify)
            {
                _mediaBrowserService.Notify(Settings, APPLICATION_UPDATE_TITLE_BRANDED, updateMessage.Message);
            }
        }

        public override void OnGameDelete(GameDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                if (Settings.Notify)
                {
                    _mediaBrowserService.Notify(Settings, GAME_DELETED_TITLE_BRANDED, deleteMessage.Message);
                }

                UpdateIfEnabled(deleteMessage.Game, Deleted);
            }
        }

        public override void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
            if (Settings.Notify)
            {
                _mediaBrowserService.Notify(Settings, GAME_FILE_DELETED_TITLE_BRANDED, deleteMessage.Message);
            }

            UpdateIfEnabled(deleteMessage.Game, Deleted);
        }

        public override void ProcessQueue()
        {
            _updateQueue.ProcessQueue(Settings.Host, (items) =>
            {
                if (Settings.UpdateLibrary)
                {
                    _logger.Debug("Performing library update for {0} games", items.Count);

                    items.ForEach(item =>
                    {
                        // If there is only one update type for the game use that, otherwise send null and let Emby decide
                        var updateType = item.Info.Count == 1 ? item.Info.First() : null;

                        _mediaBrowserService.Update(Settings, item.Game, updateType);
                    });
                }
            });
        }

        private void UpdateIfEnabled(Game game, string updateType)
        {
            if (Settings.UpdateLibrary)
            {
                _logger.Debug("Scheduling library update for game {0} {1}", game.Id, game.Title);
                _updateQueue.Add(Settings.Host, game, updateType);
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_mediaBrowserService.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
