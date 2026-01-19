using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.PushBullet
{
    public class PushBullet : NotificationBase<PushBulletSettings>
    {
        private readonly IPushBulletProxy _proxy;

        public PushBullet(IPushBulletProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Pushbullet";
        public override string Link => "https://www.pushbullet.com/";

        public override void OnGrab(GrabMessage grabMessage)
        {
            _proxy.SendNotification(GAME_GRABBED_TITLE_BRANDED, grabMessage.Message, Settings);
        }

        public override void OnDownload(DownloadMessage message)
        {
            _proxy.SendNotification(GAME_DOWNLOADED_TITLE_BRANDED, message.Message, Settings);
        }

        public override void OnGameAdded(Game game)
        {
            _proxy.SendNotification(GAME_ADDED_TITLE_BRANDED, $"{game.Title} added to library", Settings);
        }

        public override void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(GAME_FILE_DELETED_TITLE_BRANDED, deleteMessage.Message, Settings);
        }

        public override void OnGameDelete(GameDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(GAME_DELETED_TITLE_BRANDED, deleteMessage.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE_BRANDED, healthCheck.Message, Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            _proxy.SendNotification(HEALTH_RESTORED_TITLE_BRANDED, $"The following issue is now resolved: {previousCheck.Message}", Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(APPLICATION_UPDATE_TITLE_BRANDED, updateMessage.Message, Settings);
        }

        public override void OnManualInteractionRequired(ManualInteractionRequiredMessage message)
        {
            _proxy.SendNotification(MANUAL_INTERACTION_REQUIRED_TITLE_BRANDED, message.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getDevices")
            {
                // Return early if there is not an API key
                if (Settings.ApiKey.IsNullOrWhiteSpace())
                {
                    return new
                    {
                        devices = new List<object>()
                    };
                }

                Settings.Validate().Filter("ApiKey").ThrowOnError();
                var devices = _proxy.GetDevices(Settings);

                return new
                {
                    options = devices.Where(d => d.Nickname.IsNotNullOrWhiteSpace())
                                            .OrderBy(d => d.Nickname, StringComparer.InvariantCultureIgnoreCase)
                                            .Select(d => new
                                            {
                                                id = d.Id,
                                                name = d.Nickname
                                            })
                };
            }

            return new { };
        }
    }
}
