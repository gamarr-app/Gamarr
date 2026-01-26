using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Validation.Paths
{
    public class GamePathValidator
    {
        private readonly IGameService _gamesService;

        public GamePathValidator(IGameService gamesService)
        {
            _gamesService = gamesService;
        }

        public bool Validate(string value, int instanceId)
        {
            if (value == null)
            {
                return true;
            }

            // Skip the path for this game and any invalid paths
            return !_gamesService.AllGamePaths().Any(s => s.Key != instanceId &&
                                                            s.Value.IsPathValid(PathValidationType.CurrentOs) &&
                                                            s.Value.PathEquals(value));
        }
    }
}
