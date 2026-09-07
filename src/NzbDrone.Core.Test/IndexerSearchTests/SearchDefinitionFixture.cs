using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    public class SearchDefinitionFixture : CoreTest<GameSearchCriteria>
    {
        [TestCase("Betty White's Off Their Rockers", "Betty+Whites+Off+Their+Rockers")]
        [TestCase("Star Wars: The Clone Wars", "Star+Wars+The+Clone+Wars")]
        [TestCase("Hawaii Five-0", "Hawaii+Five+0")]
        [TestCase("Franklin & Bash", "Franklin+and+Bash")]
        [TestCase("Chicago P.D.", "Chicago+PD")]
        [TestCase("Kourtney And Khlo\u00E9 Take The Hamptons", "Kourtney+And+Khloe+Take+The+Hamptons")]
        [TestCase("Betty White`s Off Their Rockers", "Betty+Whites+Off+Their+Rockers")]
        [TestCase("Betty White\u00b4s Off Their Rockers", "Betty+Whites+Off+Their+Rockers")]
        [TestCase("Betty White‘s Off Their Rockers", "Betty+Whites+Off+Their+Rockers")]
        [TestCase("Betty White’s Off Their Rockers", "Betty+Whites+Off+Their+Rockers")]
        public void should_replace_some_special_characters(string input, string expected)
        {
            Subject.SceneTitles = new List<string> { input };
            Subject.CleanSceneTitles.First().Should().Be(expected);
        }

        // Deleting the apostrophe fuses the possessive onto the stem, and "Kirbys" matched
        // zero results on every configured tracker while the release itself was posted as
        // "[Nintendo Switch] Kirby's Return to Dream Land Deluxe [NSP][ENG]". Both spellings
        // are in use across trackers, so the query has to carry both.
        [TestCase("Kirby's Return to Dream Land Deluxe", "Kirbys+Return+to+Dream+Land+Deluxe", "Kirby+Return+to+Dream+Land+Deluxe")]
        [TestCase("Assassin's Creed", "Assassins+Creed", "Assassin+Creed")]
        [TestCase("Betty White’s Off Their Rockers", "Betty+Whites+Off+Their+Rockers", "Betty+White+Off+Their+Rockers")]
        public void should_query_both_possessive_forms(string input, string joined, string dropped)
        {
            Subject.SceneTitles = new List<string> { input };

            // Joined first: it stays the primary query, so this only ever adds a fallback.
            Subject.SearchQueryTitles.Should().Equal(joined, dropped);
        }

        [TestCase("Star Wars: The Clone Wars")]
        [TestCase("Chicago P.D.")]
        [TestCase("Hawaii Five-0")]
        public void should_not_add_a_second_query_when_there_is_no_possessive_to_drop(string input)
        {
            Subject.SceneTitles = new List<string> { input };

            // A title with no possessive must not cost a second request per indexer.
            Subject.SearchQueryTitles.Should().HaveCount(1);
        }
    }
}
