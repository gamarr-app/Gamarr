using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamedGameFile : WebhookGameFile
    {
        public WebhookRenamedGameFile(RenamedGameFile renamedGame)
            : base(renamedGame.GameFile)
        {
            PreviousRelativePath = renamedGame.PreviousRelativePath;
            PreviousPath = renamedGame.PreviousPath;
        }

        public string PreviousRelativePath { get; set; }
        public string PreviousPath { get; set; }
    }
}
