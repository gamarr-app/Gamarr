using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Pushcut
{
    public class Pushcut : NotificationBase<PushcutSettings>
    {
        private readonly IPushcutProxy _proxy;

        public Pushcut(IPushcutProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Pushcut";

        public override string Link => "https://www.pushcut.io";

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }

        public override void OnGrab(GrabMessage grabMessage)
        {
            _proxy.SendNotification(GAME_GRABBED_TITLE, grabMessage?.Message, GetPosterUrl(grabMessage.Game), GetLinks(grabMessage.Game), Settings);
        }

        public override void OnDownload(DownloadMessage downloadMessage)
        {
            _proxy.SendNotification(downloadMessage.OldGameFiles.Any() ? GAME_UPGRADED_TITLE : GAME_DOWNLOADED_TITLE, downloadMessage.Message, GetPosterUrl(downloadMessage.Game), GetLinks(downloadMessage.Game), Settings);
        }

        public override void OnGameAdded(Game game)
        {
            _proxy.SendNotification(GAME_ADDED_TITLE, $"{game.Title} added to library", GetPosterUrl(game), GetLinks(game), Settings);
        }

        public override void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(GAME_FILE_DELETED_TITLE, deleteMessage.Message, GetPosterUrl(deleteMessage.Game), GetLinks(deleteMessage.Game), Settings);
        }

        public override void OnGameDelete(GameDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(GAME_DELETED_TITLE, deleteMessage.Message, GetPosterUrl(deleteMessage.Game), GetLinks(deleteMessage.Game), Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE_BRANDED, healthCheck.Message, null, new List<NotificationMetadataLink>(), Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            _proxy.SendNotification(HEALTH_RESTORED_TITLE_BRANDED, $"The following issue is now resolved: {previousCheck.Message}", null, new List<NotificationMetadataLink>(), Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(APPLICATION_UPDATE_TITLE_BRANDED, updateMessage.Message, null, new List<NotificationMetadataLink>(), Settings);
        }

        public override void OnManualInteractionRequired(ManualInteractionRequiredMessage manualInteractionRequiredMessage)
        {
            _proxy.SendNotification(MANUAL_INTERACTION_REQUIRED_TITLE_BRANDED, manualInteractionRequiredMessage.Message, null, new List<NotificationMetadataLink>(), Settings);
        }

        private string GetPosterUrl(Game game)
        {
            return game.GameMetadata.Value.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Poster)?.RemoteUrl;
        }

        private List<NotificationMetadataLink> GetLinks(Game game)
        {
            return NotificationMetadataLinkGenerator.GenerateLinks(game, Settings.MetadataLinks);
        }
    }
}
