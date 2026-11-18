using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Games
{
    // Core-side duplicate check for every add path. The API's
    // GameExistsValidator only guarded the single-add POST, so library
    // import, Discover bulk adds, import lists and collections could put a
    // second same-platform row next to an existing one (AddGames cloned the
    // metadata for any platformed entry without asking which platform the
    // existing row had).
    //
    // The same title is allowed once per platform (#150). A platformed add
    // collides only with an entry of the SAME platform; an Unknown add
    // collides with any entry. An existing Unknown entry does not block a
    // platformed add: that is the upgrade path for legacy libraries.
    public class GameDuplicateValidator
    {
        private readonly IGameService _gameService;

        public GameDuplicateValidator(IGameService gameService)
        {
            _gameService = gameService;
        }

        public Game FindDuplicate(Game game)
        {
            var igdbId = game.IgdbId;
            var steamAppId = game.SteamAppId;

            var candidates = new List<Game>();

            if (igdbId > 0 || steamAppId > 0)
            {
                candidates.AddRange(_gameService.FindByIgdbIdOrSteamAppId(igdbId, steamAppId));
            }

            // The same game added once from IGDB (Steam 0) and once from Steam
            // (IGDB 0) shares no id, so fall back to title + year, but only
            // between entries whose ids are complementary.
            var steamOnly = igdbId <= 0 && steamAppId > 0;
            var igdbOnly = steamAppId <= 0 && igdbId > 0;

            if ((steamOnly || igdbOnly) && game.Year > 0 && !string.IsNullOrWhiteSpace(game.Title))
            {
                var cleanTitle = game.Title.CleanGameTitle();

                candidates.AddRange(_gameService.FindByTitleCandidates(new List<string> { game.Title }, out _)
                    .Where(g => g.Year == game.Year &&
                                g.GameMetadata?.Value?.CleanTitle == cleanTitle &&
                                ((steamOnly && g.IgdbId > 0 && g.SteamAppId <= 0) ||
                                 (igdbOnly && g.SteamAppId > 0 && g.IgdbId <= 0))));
            }

            return candidates
                .Where(g => g.Id != game.Id)
                .FirstOrDefault(g => PlatformsCollide(g.Platform, game.Platform));
        }

        public static bool PlatformsCollide(PlatformFamily existing, PlatformFamily added)
        {
            return added == PlatformFamily.Unknown || existing == added;
        }
    }
}
