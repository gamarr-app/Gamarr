using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    /// <summary>
    /// Every title below is a verbatim release name taken from
    /// GET /api/v3/release?gameId=60 (Kirby and the Forgotten Land) against the
    /// live instance - not a constructed example. Twelve of the eighteen used to
    /// come back "Unable to parse release", which happened before any platform
    /// or quality logic ran and so masked every other fix.
    ///
    /// Note the SPACE before the container in "... [v0] nsp": Prowlarr and
    /// TorrentDownload normalise the extension separator to whitespace, so a
    /// real grab never carries a literal dot there.
    /// </summary>
    [TestFixture]
    public class ConsoleTitleParserFixture : CoreTest
    {
        // The six that already parsed - they must keep parsing, unchanged.
        [TestCase("Kirby and the Forgotten Land [FitGirl Repack]")]
        [TestCase("Kirby and the Forgotten Land")]

        // The twelve that returned null.
        [TestCase("Kirby and the Forgotten Land [sakura] rar")]
        [TestCase("Kirby and the Forgotten Land (NSP)(Base Game) rar")]
        [TestCase("Kirby and the Forgotten Land (Portable)")]
        [TestCase("Kirby and the Forgotten Land [NSP]")]
        [TestCase("Kirby and the Forgotten Land [NSZ]")]
        [TestCase("Kirby and the Forgotten Land NSW VENOM")]
        [TestCase("Kirby and the Forgotten Land [v0]")]
        [TestCase("[Switch NSP] Kirby and the Forgotten Land")]
        public void should_parse_live_switch_release_to_the_bare_game_title(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be("Kirby and the Forgotten Land");
        }

        [Test]
        public void should_strip_the_switch_title_id_and_version_from_the_game_title()
        {
            var parsed = Parser.Parser.ParseGameTitle("Kirby and the Forgotten Land [01004D300C5AE000][v0] nsp");

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be("Kirby and the Forgotten Land");
            parsed.Platform.Should().Be(PlatformFamily.NintendoSwitch);
        }

        [Test]
        public void should_keep_parsing_the_release_that_already_carried_a_platform_word()
        {
            var parsed = Parser.Parser.ParseGameTitle("Kirby and the Forgotten Land - Nintendo Switch");

            parsed.Should().NotBeNull();
            parsed.Platform.Should().Be(PlatformFamily.NintendoSwitch);
        }

        // A console release must never come back as PC: Unknown means "any" and
        // is safe, but pc is the value that lets a PC repack satisfy a Switch
        // entry. Covers all eighteen live titles.
        [TestCase("Kirby and the Forgotten Land [FitGirl Repack]")]
        [TestCase("Kirby and the Forgotten Land - Nintendo Switch")]
        [TestCase("Kirby and the Forgotten Land [01004D300C5AE000][v0] nsp")]
        [TestCase("Kirby and the Forgotten Land")]
        [TestCase("Kirby and the Forgotten Land [sakura] rar")]
        [TestCase("Kirby and the Forgotten Land (NSP)(Base Game) rar")]
        [TestCase("Kirby and the Forgotten Land (Portable)")]
        [TestCase("Kirby and the Forgotten Land [NSP]")]
        [TestCase("Kirby and the Forgotten Land [NSZ]")]
        [TestCase("Kirby and the Forgotten Land NSW VENOM")]
        [TestCase("Kirby and the Forgotten Land [v0]")]
        [TestCase("[Switch NSP] Kirby and the Forgotten Land")]
        public void should_never_parse_a_live_release_as_pc(string postTitle)
        {
            Parser.Parser.ParseGameTitle(postTitle).Platform.Should().NotBe(PlatformFamily.PC);
        }

        // The bracketed container names are the only Switch marker these
        // releases carry, and bracketed they are not file extensions either, so
        // the extension fallback never sees them.
        [TestCase("Kirby and the Forgotten Land [NSP]")]
        [TestCase("Kirby and the Forgotten Land [NSZ]")]
        [TestCase("Kirby and the Forgotten Land (NSP)(Base Game) rar")]
        [TestCase("Kirby and the Forgotten Land NSW VENOM")]
        [TestCase("[Switch NSP] Kirby and the Forgotten Land")]
        [TestCase("Game Title [XCI]")]
        public void should_detect_switch_from_a_container_token(string postTitle)
        {
            PlatformParser.ParsePlatform(postTitle).Should().Be(PlatformFamily.NintendoSwitch);
            PlatformParser.ParsePlatformString(postTitle).Should().Be("Switch");
        }

        // Other console metadata shapes seen on real files.
        [TestCase("Game Title [Base]")]
        [TestCase("Game Title [UPD]")]
        [TestCase("Game Title [1.1.0]")]
        [TestCase("Game Title [US]")]
        [TestCase("Game Title (v65536) (v1.1.0) (Update)")]
        [TestCase("Game Title [0100152000022000][v65536] xci")]
        public void should_parse_console_metadata_shapes(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be("Game Title");
        }

        // Normalising is only ever a retry, so anything that does not need it
        // must be left completely alone.
        [TestCase("Kirby and the Forgotten Land")]
        [TestCase("Hytale")]
        [TestCase("ELDEN RING-PLAZA")]
        public void should_not_normalize_a_title_that_needs_no_stripping(string postTitle)
        {
            ConsoleTitleParser.Normalize(postTitle).Should().BeNull();
        }

        // A name that is nothing but tags must not collapse to an empty string.
        [TestCase("[NSP]")]
        [TestCase("[v0]")]
        [TestCase("(Base Game)")]
        public void should_not_reduce_a_tag_only_name_to_nothing(string postTitle)
        {
            var normalized = ConsoleTitleParser.Normalize(postTitle);

            if (normalized != null)
            {
                normalized.Should().NotBeEmpty();
            }
        }
    }
}
