using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRemoteGame
    {
        public int IgdbId { get; set; }
        public string ImdbId { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }

        public WebhookRemoteGame()
        {
        }

        public WebhookRemoteGame(RemoteGame remoteGame)
        {
            IgdbId = remoteGame.Game.GameMetadata.Value.IgdbId;
            ImdbId = remoteGame.Game.GameMetadata.Value.ImdbId;
            Title = remoteGame.Game.GameMetadata.Value.Title;
            Year = remoteGame.Game.GameMetadata.Value.Year;
        }

        public WebhookRemoteGame(Game game)
        {
            IgdbId = game.GameMetadata.Value.IgdbId;
            ImdbId = game.GameMetadata.Value.ImdbId;
            Title = game.GameMetadata.Value.Title;
            Year = game.GameMetadata.Value.Year;
        }
    }
}
