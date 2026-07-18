using System.Linq;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Games.Components
{
    public static class GameComponentMatcher
    {
        // A DLC release matches a slot when the slot's title appears in the
        // release title. Both sides are cleaned separator-free because the
        // scene-title cleaner strips dots entirely (they aren't word-broken),
        // so dotted release names lose their separators.
        public static bool ReleaseMatchesDlcTitle(string releaseTitle, string componentTitle)
        {
            if (string.IsNullOrWhiteSpace(releaseTitle) || string.IsNullOrWhiteSpace(componentTitle))
            {
                return false;
            }

            var cleanRelease = Clean(releaseTitle);
            var cleanComponent = Clean(componentTitle);

            return cleanComponent.Length > 0 && cleanRelease.Contains(cleanComponent);
        }

        private static string Clean(string title)
        {
            return new string(SearchCriteriaBase.GetCleanSceneTitle(title)
                .Where(char.IsLetterOrDigit)
                .ToArray()).ToLowerInvariant();
        }
    }
}
