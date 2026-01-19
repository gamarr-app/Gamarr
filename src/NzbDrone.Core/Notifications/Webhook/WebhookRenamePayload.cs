using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamePayload : WebhookPayload
    {
        public WebhookGame Game { get; set; }
        public List<WebhookRenamedGameFile> RenamedGameFiles { get; set; }
    }
}
