using System.Linq;
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
            return Validate(igdbId, PlatformFamily.Unknown);
        }

        public bool ValidateSteamAppId(int steamAppId)
        {
            return ValidateSteamAppId(steamAppId, PlatformFamily.Unknown);
        }

        // The same title may exist once per platform (#150). A platformed add
        // collides only with an entry of the SAME platform — existing
        // platform-agnostic entries don't block it (that's the upgrade path
        // for legacy libraries). An agnostic (Unknown) add collides with any
        // existing entry, so plain duplicates stay impossible.
        public bool Validate(int igdbId, PlatformFamily platform)
        {
            if (igdbId == 0)
            {
                return true;
            }

            return !_gameService.GetAllGames()
                .Any(g => g.IgdbId == igdbId && PlatformsCollide(g.Platform, platform));
        }

        public bool ValidateSteamAppId(int steamAppId, PlatformFamily platform)
        {
            if (steamAppId == 0)
            {
                return true;
            }

            return !_gameService.GetAllGames()
                .Any(g => g.SteamAppId == steamAppId && PlatformsCollide(g.Platform, platform));
        }

        private static bool PlatformsCollide(PlatformFamily existing, PlatformFamily added)
        {
            return added == PlatformFamily.Unknown || existing == added;
        }
    }
}
