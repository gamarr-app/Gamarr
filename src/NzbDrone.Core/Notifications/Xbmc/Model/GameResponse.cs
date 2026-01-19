namespace NzbDrone.Core.Notifications.Xbmc.Model
{
    public class GameResponse
    {
        public string Id { get; set; }
        public string JsonRpc { get; set; }
        public GameResult Result { get; set; }
    }
}
