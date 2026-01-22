using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Telegram
{
    public class Telegram : NotificationBase<TelegramSettings>
    {
        private readonly ITelegramProxy _proxy;
        private readonly IConfigFileProvider _configFileProvider;

        public Telegram(ITelegramProxy proxy, IConfigFileProvider configFileProvider)
        {
            _proxy = proxy;
            _configFileProvider = configFileProvider;
        }

        public override string Name => "Telegram";
        public override string Link => "https://telegram.org/";

        private string InstanceName => _configFileProvider.InstanceName;

        public override void OnGrab(GrabMessage grabMessage)
        {
            var title = Settings.IncludeAppNameInTitle ? GAME_GRABBED_TITLE_BRANDED : GAME_GRABBED_TITLE;
            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;
            var links = GetLinks(grabMessage.Game);

            _proxy.SendNotification(title, grabMessage.Message, links, Settings);
        }

        public override void OnDownload(DownloadMessage message)
        {
            string title;
            if (message.OldGameFiles.Any())
            {
                title = Settings.IncludeAppNameInTitle ? GAME_UPGRADED_TITLE_BRANDED : GAME_UPGRADED_TITLE;
            }
            else
            {
                title = Settings.IncludeAppNameInTitle ? GAME_DOWNLOADED_TITLE_BRANDED : GAME_DOWNLOADED_TITLE;
            }

            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;
            var links = GetLinks(message.Game);

            _proxy.SendNotification(title, message.Message, links, Settings);
        }

        public override void OnGameAdded(Game game)
        {
            var title = Settings.IncludeAppNameInTitle ? GAME_ADDED_TITLE_BRANDED : GAME_ADDED_TITLE;
            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;
            var links = GetLinks(game);

            _proxy.SendNotification(title, $"{game.Title} added to library", links, Settings);
        }

        public override void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
            var title = Settings.IncludeAppNameInTitle ? GAME_FILE_DELETED_TITLE_BRANDED : GAME_FILE_DELETED_TITLE;
            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;
            var links = GetLinks(deleteMessage.Game);

            _proxy.SendNotification(title, deleteMessage.Message, links, Settings);
        }

        public override void OnGameDelete(GameDeleteMessage deleteMessage)
        {
            var title = Settings.IncludeAppNameInTitle ? GAME_DELETED_TITLE_BRANDED : GAME_DELETED_TITLE;
            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;
            var links = GetLinks(deleteMessage.Game);

            _proxy.SendNotification(title, deleteMessage.Message, links, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var title = Settings.IncludeAppNameInTitle ? HEALTH_ISSUE_TITLE_BRANDED : HEALTH_ISSUE_TITLE;
            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;

            _proxy.SendNotification(title, healthCheck.Message, new List<TelegramLink>(), Settings);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            var title = Settings.IncludeAppNameInTitle ? HEALTH_RESTORED_TITLE_BRANDED : HEALTH_RESTORED_TITLE;
            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;

            _proxy.SendNotification(title, $"The following issue is now resolved: {previousCheck.Message}", new List<TelegramLink>(), Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var title = Settings.IncludeAppNameInTitle ? APPLICATION_UPDATE_TITLE_BRANDED : APPLICATION_UPDATE_TITLE;
            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;

            _proxy.SendNotification(title, updateMessage.Message, new List<TelegramLink>(), Settings);
        }

        public override void OnManualInteractionRequired(ManualInteractionRequiredMessage message)
        {
            var title = Settings.IncludeAppNameInTitle ? MANUAL_INTERACTION_REQUIRED_TITLE_BRANDED : MANUAL_INTERACTION_REQUIRED_TITLE;
            title = Settings.IncludeInstanceNameInTitle ? $"{title} - {InstanceName}" : title;

            _proxy.SendNotification(title, message.Message, new List<TelegramLink>(), Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }

        private List<TelegramLink> GetLinks(Game game)
        {
            var links = new List<TelegramLink>();

            if (game == null)
            {
                return links;
            }

            foreach (var link in Settings.MetadataLinks)
            {
                var linkType = (MetadataLinkType)link;

                // IGDB - Internet Game Database (primary link for games)
                var igdbSlug = game.GameMetadata?.Value?.IgdbSlug;
                if (linkType == MetadataLinkType.Igdb && !string.IsNullOrEmpty(igdbSlug))
                {
                    links.Add(new TelegramLink("IGDB", $"https://www.igdb.com/games/{igdbSlug}"));
                }

                if (linkType == MetadataLinkType.Steam && game.GameMetadata?.Value?.SteamAppId > 0)
                {
                    links.Add(new TelegramLink("Steam", $"https://store.steampowered.com/app/{game.GameMetadata.Value.SteamAppId}"));
                }

                {
                }
            }

            return links;
        }
    }
}
