using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    /// <summary>
    /// DLC component releases on a Switch tracker: "&lt;Game&gt; Wave 1-6 DLC
    /// Unlocker NSW-&lt;GROUP&gt;" came back "Unable to parse release" while
    /// "&lt;Game&gt; Wave 1 DLC NSW-&lt;GROUP&gt;" parsed, so the whole release was
    /// rejected before any quality or decision logic ran.
    ///
    /// Two independent faults, both in the terminal bare-title fallback:
    /// the range "1-6" is none of its word shapes (a hyphen-joined word has to
    /// lead with a capital), and "DLC" only ever matched it as the trailing
    /// Roman-numeral group - D, L and C are all numerals - which means it had to
    /// be the LAST token. That is the whole difference between "Wave 1 DLC" and
    /// "Wave 1 DLC Unlocker".
    /// </summary>
    [TestFixture]
    public class DlcComponentTitleParserFixture : CoreTest
    {
        private const string Title = "Kirby and the Forgotten Land";

        [Test]
        public void should_parse_the_dlc_wave_range_unlocker_release_from_the_issue()
        {
            var parsed = Parser.Parser.ParseGameTitle($"{Title} Wave 1-6 DLC Unlocker NSW-VENOM");

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be($"{Title} Wave 1-6 DLC Unlocker");
            parsed.ContentType.Should().Be(ReleaseContentType.SeasonPass);
            parsed.Platform.Should().Be(PlatformFamily.NintendoSwitch);
        }

        // The shape that already parsed, as a regression guard: it must keep
        // parsing, keep its title and stay plain DLC rather than a bundle.
        [Test]
        public void should_keep_parsing_the_single_wave_dlc_release()
        {
            var parsed = Parser.Parser.ParseGameTitle($"{Title} Wave 1 DLC NSW-VENOM");

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be($"{Title} Wave 1 DLC");
            parsed.ContentType.Should().Be(ReleaseContentType.DlcOnly);
        }

        // Adjacent shapes: a range on its own, and the Unlocker wording on its
        // own - each fault in isolation, so a later change cannot fix one and
        // quietly lose the other.
        [Test]
        public void should_parse_a_wave_range_without_the_unlocker_wording()
        {
            var parsed = Parser.Parser.ParseGameTitle($"{Title} Wave 2-4 DLC NSW-VENOM");

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be($"{Title} Wave 2-4 DLC");
            parsed.ContentType.Should().Be(ReleaseContentType.DlcOnly);
        }

        [Test]
        public void should_parse_the_unlocker_wording_without_a_wave_range()
        {
            var parsed = Parser.Parser.ParseGameTitle($"{Title} DLC Unlocker NSW-VENOM");

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be($"{Title} DLC Unlocker");
            parsed.ContentType.Should().Be(ReleaseContentType.SeasonPass);
        }

        // The fix must not reach the bare dump tokens. If the fallback's word
        // class had been widened to all-caps tokens instead, this name would
        // parse here with "NSW VENOM" glued onto the game title rather than
        // being normalised away by the console retry - and NSW is a token that
        // mostly means something else entirely.
        [Test]
        public void should_still_strip_bare_console_dump_tokens_rather_than_keep_them()
        {
            var parsed = Parser.Parser.ParseGameTitle($"{Title} NSW VENOM");

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be(Title);
        }
    }
}
