using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Xbmc.Model
{
    public class GameResult
    {
        public Dictionary<string, int> Limits { get; set; }
        public List<XbmcGame> Games;

        public GameResult()
        {
            Games = new List<XbmcGame>();
        }
    }
}
