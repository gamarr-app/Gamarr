using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookGameFileDeletePayload : WebhookPayload
    {
        public WebhookGame Game { get; set; }
        public WebhookGameFile GameFile { get; set; }
        public DeleteMediaFileReason DeleteReason { get; set; }
    }
}
