using NzbDrone.Core.Games;

namespace NzbDrone.Core.Validation.Paths
{
    public class GameExistsValidator
    {
        private readonly IGameService _gameService;

        public GameExistsValidator(IGameService gameService)
        {
            _gameService = gameService;
        }

        public bool Validate(int igdbId)
        {
            if (igdbId == 0)
            {
                return true;
            }

            return _gameService.FindByIgdbId(igdbId) == null;
        }

        public bool ValidateSteamAppId(int steamAppId)
        {
            if (steamAppId == 0)
            {
                return true;
            }

            return _gameService.FindBySteamAppId(steamAppId) == null;
        }
    }
}
