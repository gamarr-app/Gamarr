using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Apprise
{
    public class Apprise : NotificationBase<AppriseSettings>
    {
        public override string Name => "Apprise";

        public override string Link => "https://github.com/caronc/apprise";

        private readonly IAppriseProxy _proxy;

        public Apprise(IAppriseProxy proxy)
        {
            _proxy = proxy;
        }

        public override void OnGrab(GrabMessage grabMessage)
        {
            _proxy.SendNotification(GAME_GRABBED_TITLE, grabMessage.Message, GetPosterUrl(grabMessage.Game), Settings);
        }

        public override void OnDownload(DownloadMessage message)
        {
            _proxy.SendNotification(message.OldGameFiles.Any() ? GAME_UPGRADED_TITLE : GAME_DOWNLOADED_TITLE, message.Message, GetPosterUrl(message.Game), Settings);
        }

        public override void OnGameAdded(Game game)
        {
            _proxy.SendNotification(GAME_ADDED_TITLE, $"{game.Title} ({game.Year}) added to library", GetPosterUrl(game), Settings);
        }

        public override void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(GAME_FILE_DELETED_TITLE, deleteMessage.Message, GetPosterUrl(deleteMessage.Game), Settings);
        }

        public override void OnGameDelete(GameDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(GAME_DELETED_TITLE, deleteMessage.Message, GetPosterUrl(deleteMessage.Game), Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE, healthCheck.Message, null, Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            _proxy.SendNotification(HEALTH_RESTORED_TITLE, $"The following issue is now resolved: {previousCheck.Message}", null, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(APPLICATION_UPDATE_TITLE, updateMessage.Message, null, Settings);
        }

        public override void OnManualInteractionRequired(ManualInteractionRequiredMessage message)
        {
            _proxy.SendNotification(MANUAL_INTERACTION_REQUIRED_TITLE, message.Message, GetPosterUrl(message.Game), Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }

        private static string GetPosterUrl(Game game)
        {
            return game?.GameMetadata?.Value?.Images?.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Poster)?.RemoteUrl;
        }
    }
}
