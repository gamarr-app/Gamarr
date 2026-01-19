namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookGameDeletePayload : WebhookPayload
    {
        public WebhookGame Game { get; set; }
        public bool DeletedFiles { get; set; }
        public long GameFolderSize { get; set; }
    }
}
