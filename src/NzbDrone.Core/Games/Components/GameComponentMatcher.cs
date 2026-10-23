using System;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Games.Components
{
    public static class GameComponentMatcher
    {
        // A slot name shorter than this is not evidence of anything: two-letter
        // and three-letter remainders ("II", "DX") turn up inside unrelated
        // release names constantly, and the remainder comparison below is a
        // containment test with no word boundaries to protect it.
        private const int MinimumRemainderLength = 4;

        // The separators IGDB and the stores use between a game's title and the
        // name of one of its add-ons: "<Game>: <Name>", "<Game> - <Name>".
        private static readonly char[] NamePartSeparators = { ':' };

        // A DLC release matches a slot when the slot's title appears in the
        // release title. Both sides are cleaned separator-free because the
        // scene-title cleaner strips dots entirely (they aren't word-broken),
        // so dotted release names lose their separators.
        //
        // Pass <paramref name="gameTitle"/> wherever the game is known: it
        // enables the remainder comparison described on RemainderMatches, which
        // is what lets a release be mapped to the right slot when the two spell
        // the shared game prefix, or the add-on's articles, differently (#212).
        public static bool ReleaseMatchesDlcTitle(string releaseTitle, string componentTitle, string gameTitle = null)
        {
            if (releaseTitle.IsNullOrWhiteSpace() || componentTitle.IsNullOrWhiteSpace())
            {
                return false;
            }

            var cleanRelease = Clean(releaseTitle);
            var cleanComponent = Clean(componentTitle);

            if (cleanComponent.Length > 0 && cleanRelease.Contains(cleanComponent))
            {
                return true;
            }

            return RemainderMatches(releaseTitle, componentTitle, gameTitle);
        }

        /// <summary>
        /// Compare only what each side says AFTER the game's title: the slot's
        /// own name against the release's remainder.
        /// </summary>
        /// <remarks>
        /// The containment test above needs the slot's whole title to appear
        /// verbatim in the release name, which fails on the two things that
        /// routinely differ between a metadata slot title and a scene release
        /// name even when both name the same add-on:
        ///
        ///  * the shared game prefix - the slot writes "&lt;Game&gt;: &lt;Name&gt;" or
        ///    "&lt;Game&gt; - &lt;Name&gt;" while the release just runs the words
        ///    together, so any difference in the game part (a dropped article, a
        ///    subtitle the release omits) sinks the whole comparison;
        ///  * articles inside the add-on's name - a slot called
        ///    "&lt;Game&gt;: The &lt;Name&gt; Expansion" never matched a release
        ///    called "&lt;Game&gt; &lt;Name&gt; Expansion DLC".
        ///
        /// So strip the game prefix off both sides and compare the remainders
        /// through CleanGameTitle, which drops articles and punctuation. The
        /// release remainder keeps its trailing metadata (markers, region,
        /// group), hence containment rather than equality: the slot name is
        /// expected to be a leading part of it, not all of it.
        /// </remarks>
        private static bool RemainderMatches(string releaseTitle, string componentTitle, string gameTitle)
        {
            var componentName = NamePart(componentTitle, gameTitle);

            if (componentName.IsNullOrWhiteSpace() || componentName.Length < MinimumRemainderLength)
            {
                return false;
            }

            var releaseName = NamePart(releaseTitle, gameTitle);

            // No game title to strip, or the release does not lead with it: the
            // containment test already had its chance on the full name and
            // there is nothing further to compare.
            if (releaseName.IsNullOrWhiteSpace())
            {
                return false;
            }

            return releaseName.Contains(componentName);
        }

        /// <summary>
        /// The add-on's own name within <paramref name="title"/>, cleaned for
        /// comparison, or null when the title carries no recognisable game
        /// prefix to strip.
        /// </summary>
        private static string NamePart(string title, string gameTitle)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            // An explicit separator is the strongest signal and needs no game
            // title, so a slot title is usable even where the caller has none.
            // Only the FIRST separator splits, so a name that itself contains
            // one keeps the rest of it.
            var separatorIndex = title.IndexOfAny(NamePartSeparators);
            var dashIndex = title.IndexOf(" - ", StringComparison.Ordinal);

            if (dashIndex >= 0 && (separatorIndex < 0 || dashIndex < separatorIndex))
            {
                return CleanName(title.Substring(dashIndex + 3));
            }

            if (separatorIndex >= 0)
            {
                return CleanName(title.Substring(separatorIndex + 1));
            }

            if (gameTitle.IsNullOrWhiteSpace())
            {
                return null;
            }

            // No separator, so fall back to consuming the game's title off the
            // front word by word - the same rule ParsingService uses to decide
            // where a game's title ends and an add-on's name begins.
            return CleanName(NzbDrone.Core.Parser.Parser.GetTitleRemainder(title, gameTitle.CleanGameTitle()));
        }

        private static string CleanName(string name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return null;
            }

            var cleaned = name.CleanGameTitle();

            return cleaned.IsNullOrWhiteSpace() ? null : cleaned;
        }

        private static string Clean(string title)
        {
            return new string(SearchCriteriaBase.GetCleanSceneTitle(title)
                .Where(char.IsLetterOrDigit)
                .ToArray()).ToLowerInvariant();
        }
    }
}
