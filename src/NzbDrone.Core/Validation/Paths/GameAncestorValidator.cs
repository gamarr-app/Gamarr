using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Validation.Paths
{
    public class GameAncestorValidator
    {
        private readonly IGameService _gameService;

        public GameAncestorValidator(IGameService gameService)
        {
            _gameService = gameService;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return true;
            }

            return !_gameService.AllGamePaths().Any(s => value.IsParentPath(s.Value));
        }
    }
}
