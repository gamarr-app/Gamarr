using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Gotify
{
    public class Gotify : NotificationBase<GotifySettings>
    {
        private const string GamarrImageUrl = "https://raw.githubusercontent.com/Gamarr/Gamarr/develop/Logo/128.png";

        private readonly IGotifyProxy _proxy;
        private readonly ILocalizationService _localizationService;
        private readonly Logger _logger;

        public Gotify(IGotifyProxy proxy, ILocalizationService localizationService, Logger logger)
        {
            _proxy = proxy;
            _localizationService = localizationService;
            _logger = logger;
        }

        public override string Name => "Gotify";
        public override string Link => "https://gotify.net/";

        public override void OnGrab(GrabMessage message)
        {
            SendNotification(GAME_GRABBED_TITLE, message.Message, message.Game);
        }

        public override void OnDownload(DownloadMessage message)
        {
            SendNotification(GAME_DOWNLOADED_TITLE, message.Message, message.Game);
        }

        public override void OnGameAdded(Game game)
        {
            SendNotification(GAME_ADDED_TITLE, $"{game.Title} added to library", game);
        }

        public override void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
            SendNotification(GAME_FILE_DELETED_TITLE, deleteMessage.Message, deleteMessage.Game);
        }

        public override void OnGameDelete(GameDeleteMessage deleteMessage)
        {
            SendNotification(GAME_DELETED_TITLE, deleteMessage.Message, deleteMessage.Game);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            SendNotification(HEALTH_ISSUE_TITLE, healthCheck.Message, null);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            SendNotification(HEALTH_RESTORED_TITLE, $"The following issue is now resolved: {previousCheck.Message}", null);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage message)
        {
            SendNotification(APPLICATION_UPDATE_TITLE, message.Message, null);
        }

        public override void OnManualInteractionRequired(ManualInteractionRequiredMessage message)
        {
            SendNotification(MANUAL_INTERACTION_REQUIRED_TITLE, message.Message, message.Game);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var isMarkdown = false;
                const string title = "Test Notification";

                var sb = new StringBuilder();
                sb.AppendLine("This is a test message from Gamarr");

                var payload = new GotifyMessage
                {
                    Title = title,
                    Priority = Settings.Priority
                };

                if (Settings.IncludeGamePoster)
                {
                    isMarkdown = true;

                    sb.AppendLine($"\r![]({GamarrImageUrl})");
                    payload.SetImage(GamarrImageUrl);
                }

                if (Settings.MetadataLinks.Any())
                {
                    isMarkdown = true;

                    sb.AppendLine("");
                    sb.AppendLine("[Gamarr.video](https://gamarr.video)");
                    payload.SetClickUrl("https://gamarr.video");
                }

                payload.Message = sb.ToString();
                payload.SetContentType(isMarkdown);

                _proxy.SendNotification(payload, Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message");
                failures.Add(new ValidationFailure(string.Empty, _localizationService.GetLocalizedString("NotificationsValidationUnableToSendTestMessage", new Dictionary<string, object> { { "exceptionMessage", ex.Message } })));
            }

            return new ValidationResult(failures);
        }

        private void SendNotification(string title, string message, Game game)
        {
            var isMarkdown = false;
            var sb = new StringBuilder();

            sb.AppendLine(message);

            var payload = new GotifyMessage
            {
                Title = title,
                Priority = Settings.Priority
            };

            if (game != null)
            {
                if (Settings.IncludeGamePoster)
                {
                    var poster = game.GameMetadata.Value.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Poster)?.RemoteUrl;

                    if (poster != null)
                    {
                        isMarkdown = true;
                        sb.AppendLine($"\r![]({poster})");
                        payload.SetImage(poster);
                    }
                }

                if (Settings.MetadataLinks.Any())
                {
                    isMarkdown = true;
                    sb.AppendLine("");

                    foreach (var link in Settings.MetadataLinks)
                    {
                        var linkType = (MetadataLinkType)link;
                        var linkText = "";
                        var linkUrl = "";

                        // IGDB - Internet Game Database (primary link for games)
                        if (linkType == MetadataLinkType.Igdb && game.IgdbId > 0)
                        {
                            linkText = "IGDB";
                            linkUrl = $"https://www.igdb.com/games/{game.IgdbId}";
                        }

                        if (linkType == MetadataLinkType.Steam && game.GameMetadata?.Value?.SteamId > 0)
                        {
                            linkText = "Steam";
                            linkUrl = $"https://store.steampowered.com/app/{game.GameMetadata.Value.SteamId}";
                        }

                        if (linkType == MetadataLinkType.Rawg && game.GameMetadata?.Value?.RawgId > 0)
                        {
                            linkText = "RAWG";
                            linkUrl = $"https://rawg.io/games/{game.GameMetadata.Value.RawgId}";
                        }

                        if (linkText.Length > 0 && linkUrl.Length > 0)
                        {
                            sb.AppendLine($"[{linkText}]({linkUrl})");

                            if (link == Settings.PreferredMetadataLink)
                            {
                                payload.SetClickUrl(linkUrl);
                            }
                        }
                    }
                }
            }

            payload.Message = sb.ToString();
            payload.SetContentType(isMarkdown);

            _proxy.SendNotification(payload, Settings);
        }
    }
}
