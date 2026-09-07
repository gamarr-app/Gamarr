using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public abstract class SearchCriteriaBase
    {
        private static readonly Regex SpecialCharacter = new Regex(@"['.\u0060\u00B4\u2018\u2019]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NonWord = new Regex(@"[\W]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BeginningThe = new Regex(@"^the\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PossessiveSuffix = new Regex(@"['`´‘’]s\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Game Game { get; set; }
        public List<string> SceneTitles { get; set; }
        public virtual bool MonitoredEpisodesOnly { get; set; }
        public virtual bool UserInvokedSearch { get; set; }
        public virtual bool InteractiveSearch { get; set; }

        public List<string> CleanSceneTitles => SceneTitles.Select(GetCleanSceneTitle).Distinct().ToList();

        // Titles for an outbound indexer query, which is not the same job as cleaning a
        // title for matching. Trackers do not agree on how a possessive is written:
        // "Kirby's Return to Dream Land Deluxe" keeps the apostrophe in the release name,
        // while "Assassin's Creed" is conventionally posted as "Assassins Creed". So send
        // both forms instead of betting on one — and only for titles that actually carry a
        // possessive, so the extra request is not charged against every search.
        public List<string> SearchQueryTitles => SceneTitles.SelectMany(GetSearchQueryTitles).Distinct().ToList();

        public static IEnumerable<string> GetSearchQueryTitles(string title)
        {
            var joined = GetCleanSceneTitle(title);

            yield return joined;

            // Deleting the apostrophe alone fuses the possessive onto the stem
            // ("Kirby's" -> "Kirbys"), a token that matches nothing on any tracker when
            // the release itself spells the apostrophe out. Dropping the whole token
            // recovers those releases; keeping the joined form above preserves the
            // trackers that write it the other way.
            var withoutPossessive = PossessiveSuffix.Replace(title, string.Empty);

            if (withoutPossessive == title || withoutPossessive.IsNullOrWhiteSpace())
            {
                yield break;
            }

            var dropped = GetCleanSceneTitle(withoutPossessive);

            if (dropped != joined)
            {
                yield return dropped;
            }
        }

        public static string GetCleanSceneTitle(string title)
        {
            Ensure.That(title, () => title).IsNotNullOrWhiteSpace();

            var cleanTitle = BeginningThe.Replace(title, string.Empty);

            cleanTitle = cleanTitle.Replace("&", "and");
            cleanTitle = SpecialCharacter.Replace(cleanTitle, "");
            cleanTitle = NonWord.Replace(cleanTitle, "+");

            // remove any repeating +s
            cleanTitle = Regex.Replace(cleanTitle, @"\+{2,}", "+");
            cleanTitle = cleanTitle.RemoveAccent();
            return cleanTitle.Trim('+', ' ');
        }
    }
}
