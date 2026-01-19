using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaCover
{
    public class MediaCoversUpdatedEvent : IEvent
    {
        public Game Game { get; set; }
        public bool Updated { get; set; }

        public MediaCoversUpdatedEvent(Game game, bool updated)
        {
            Game = game;
            Updated = updated;
        }
    }
}
