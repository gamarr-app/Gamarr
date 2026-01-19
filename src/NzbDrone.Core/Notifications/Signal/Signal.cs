using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Signal
{
    public class Signal : NotificationBase<SignalSettings>
    {
        private readonly ISignalProxy _proxy;

        public Signal(ISignalProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Signal";
        public override string Link => "https://signal.org/";

        public override void OnGrab(GrabMessage grabMessage)
        {
            _proxy.SendNotification(GAME_GRABBED_TITLE, grabMessage.Message, Settings);
        }

        public override void OnDownload(DownloadMessage message)
        {
            _proxy.SendNotification(GAME_DOWNLOADED_TITLE, message.Message, Settings);
        }

        public override void OnGameAdded(Game game)
        {
            _proxy.SendNotification(GAME_ADDED_TITLE, $"{game.Title} added to library", Settings);
        }

        public override void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(GAME_FILE_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnGameDelete(GameDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(GAME_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE, healthCheck.Message, Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            _proxy.SendNotification(HEALTH_RESTORED_TITLE, $"The following issue is now resolved: {previousCheck.Message}", Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(APPLICATION_UPDATE_TITLE, updateMessage.Message, Settings);
        }

        public override void OnManualInteractionRequired(ManualInteractionRequiredMessage message)
        {
            _proxy.SendNotification(MANUAL_INTERACTION_REQUIRED_TITLE, message.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
