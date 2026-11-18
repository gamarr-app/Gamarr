using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Games
{
    // The same title may be in the library once per platform (#150), so a
    // title lookup can legitimately return several entries. The release's
    // parsed platform picks between them, in the same order
    // PlatformSpecification accepts releases: an exact platform match, then
    // a family match (a "Nintendo"-tagged release fits a Switch entry), then
    // for untagged releases a PC-family entry, then a platform-agnostic
    // (Unknown) entry. Returns null when that still leaves no single entry.
    public static class GamePlatformMatcher
    {
        public static Game SelectForReleasePlatform(IReadOnlyCollection<Game> games, PlatformFamily releasePlatform)
        {
            if (games == null || games.Count == 0)
            {
                return null;
            }

            if (games.Count == 1)
            {
                return games.First();
            }

            if (releasePlatform != PlatformFamily.Unknown)
            {
                var single = Single(games.Where(g => g.Platform == releasePlatform)) ??
                             Single(games.Where(g => g.Platform != PlatformFamily.Unknown &&
                                                     GamePlatform.PlatformMatches(g.Platform, releasePlatform)));

                if (single != null)
                {
                    return single;
                }

                // No entry claims the release's platform at all: only an
                // agnostic entry can take it.
                if (!games.Any(g => g.Platform != PlatformFamily.Unknown &&
                                    GamePlatform.PlatformMatches(g.Platform, releasePlatform)))
                {
                    return Single(games.Where(g => g.Platform == PlatformFamily.Unknown));
                }

                return null;
            }

            return Single(games.Where(g => g.Platform is PlatformFamily.PC or PlatformFamily.Linux or PlatformFamily.Mac)) ??
                   Single(games.Where(g => g.Platform == PlatformFamily.Unknown));
        }

        private static Game Single(IEnumerable<Game> games)
        {
            var list = games.Take(2).ToList();

            return list.Count == 1 ? list[0] : null;
        }
    }
}
