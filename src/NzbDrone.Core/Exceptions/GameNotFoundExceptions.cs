using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Exceptions
{
    public class GameNotFoundException : NzbDroneException
    {
        public int IgdbGameId { get; set; }

        public GameNotFoundException(int igdbGameId)
            : base(string.Format("Game with IGDB ID {0} was not found, it may have been removed from IGDB.", igdbGameId))
        {
            IgdbGameId = igdbGameId;
        }

        public GameNotFoundException(int igdbGameId, string message, params object[] args)
            : base(message, args)
        {
            IgdbGameId = igdbGameId;
        }

        public GameNotFoundException(int igdbGameId, string message)
            : base(message)
        {
            IgdbGameId = igdbGameId;
        }
    }
}
