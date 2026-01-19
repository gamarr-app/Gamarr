using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookAddedPayload : WebhookPayload
    {
        public WebhookGame Game { get; set; }
        public AddGameMethod AddMethod { get; set; }
    }
}
