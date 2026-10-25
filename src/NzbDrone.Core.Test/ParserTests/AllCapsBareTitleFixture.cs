using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    /// <summary>
    /// A space-separated console release whose game title holds an ALL-CAPS word
    /// - a brand or an acronym, which is very common in real game names - was
    /// "Unable to parse release", so the base game was never grabbed while its
    /// own "Update vX" release parsed fine (#222).
    ///
    /// Such names match no shaped regex, so they fall to the console retry /
    /// region strip, which correctly hands the bare title back. The only regex
    /// that can consume a bare title is the terminal fallback, and its word class
    /// rejects both "[A-Z]{2,}" and digit-led tokens like "2K24" on purpose: that
    /// is what stops "&lt;Game&gt; NSW VENOM" matching with the dump tokens glued onto
    /// the title. The fix offers a permissive word class ONLY on a pass whose
    /// title came out of ConsoleTitleParser, where those tokens are already gone.
    /// </summary>
    [TestFixture]
    public class AllCapsBareTitleFixture : CoreTest
    {
        // Every row of the issue's table, with the names kept as reported.
        [TestCase("ABCD Aaaaa Bbbbb Ccccc NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc")]
        [TestCase("Abcd Aaaaa Bbbbb Ccccc NSW-GROUPX", "Abcd Aaaaa Bbbbb Ccccc")]
        [TestCase("Aaaaa ABCD Ccccc NSW-GROUPX", "Aaaaa ABCD Ccccc")]
        [TestCase("ABC 23 NSW-GROUPX", "ABC 23")]
        [TestCase("Aaaaa Bbbbb 2K24 NSW-GROUPX", "Aaaaa Bbbbb 2K24")]
        [TestCase("ABCD Aaaaa Bbbbb Ccccc EUR NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc")]
        [TestCase("ABCD Aaaaa Bbbbb Ccccc Update v1.0.1 NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc")]
        [TestCase("ABCD_Aaaaa_Bbbbb_Ccccc_NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc")]
        [TestCase("ABCD.Aaaaa.Bbbbb.Ccccc.NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc")]
        [TestCase("ABCD NSW-GROUPX", "ABCD")]
        public void should_parse_an_all_caps_console_release_to_the_bare_game_title(string postTitle, string expected)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be(expected);
        }

        // The DLC row of the same table. The component shape captures the marker
        // as part of the title, exactly as it already does for a title whose
        // words are all mixed case, so the two rows are asserted side by side -
        // the point of the test is that the all-caps row no longer behaves
        // differently from the mixed-case one.
        [TestCase("ABCD Aaaaa Bbbbb Ccccc DLC NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc DLC")]
        [TestCase("Abcd Aaaaa Bbbbb Ccccc DLC NSW-GROUPX", "Abcd Aaaaa Bbbbb Ccccc DLC")]
        [TestCase("ABCD Aaaaa Bbbbb Ccccc DLC Unlocker NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc DLC Unlocker")]
        public void should_parse_an_all_caps_dlc_component_release(string postTitle, string expected)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be(expected);
        }

        // All-caps words in any position, and the digit-led token shapes the
        // issue calls out, on names that reach the normalised pass.
        [TestCase("NBA Aaaaa Bbbbb NSW-GROUPX", "NBA Aaaaa Bbbbb")]
        [TestCase("Aaaaa WRC Bbbbb NSW-GROUPX", "Aaaaa WRC Bbbbb")]
        [TestCase("Aaaaa Bbbbb WRC24 NSW-GROUPX", "Aaaaa Bbbbb WRC24")]
        [TestCase("Aaaaa 2K Bbbbb NSW-GROUPX", "Aaaaa 2K Bbbbb")]
        public void should_accept_an_all_caps_or_digit_led_word_anywhere_in_the_title(string postTitle, string expected)
        {
            Parser.Parser.ParseGameTitle(postTitle).GameTitle.Should().Be(expected);
        }

        // REGRESSION GUARD: the dump, platform, region and group tokens must
        // still come OFF the title, not be read as all-caps title words.
        [TestCase("Kirby and the Forgotten Land NSW VENOM", "Kirby and the Forgotten Land")]
        [TestCase("Pikmin 4 EUR NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 JPN NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 USA NSW-LiGHTFORCE", "Pikmin 4")]
        [TestCase("Pikmin 4 EUR NSW", "Pikmin 4")]
        public void should_not_keep_a_dump_or_region_token_inside_the_title(string postTitle, string expected)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be(expected);
        }

        // REGRESSION GUARD: a leftover all-caps scene tag the normalisers do not
        // anchor on must not be absorbed into the title either. The region strip
        // of "<Game> REPACK NSW-GRP" anchors on NSW and hands back
        // "<Game> REPACK", so this is the case the metadata-word exclusion in the
        // permissive class exists for.
        [TestCase("Pikmin 4 REPACK NSW-VENOM")]
        [TestCase("Pikmin 4 INTERNAL NSW-VENOM")]
        [TestCase("Pikmin 4 PROPER NSW-VENOM")]
        public void should_not_absorb_a_leftover_all_caps_scene_tag(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            if (parsed != null)
            {
                parsed.GameTitle.Should().Be("Pikmin 4");
            }
        }

        // REGRESSION GUARD (#207, #212): the mixed-case scene tags keep whatever
        // handling they have today - the permissive class is upper case only, so
        // it can neither help nor hurt them.
        [TestCase("Pikmin 4 DLC Pack 2 EUR MULTi5 NSW-GRP")]
        [TestCase("Pikmin.4.MULTi5.NSW-VENOM")]
        [TestCase("Pikmin.4.iNTERNAL.NSW-VENOM")]
        [TestCase("Pikmin.4.READNFO.NSW-VENOM")]
        [TestCase("Pikmin.4.KIOSK.eShop.INTERNAL.NSW-VENOM")]
        public void should_keep_the_current_handling_of_mixed_case_scene_tags(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            if (parsed != null)
            {
                parsed.GameTitle.Should().NotContain("NSW");
                parsed.GameTitle.Should().NotContain("MULTi");
                parsed.GameTitle.Should().NotContain("eShop");
            }
        }

        // REGRESSION GUARD: a raw name never reaches the permissive pass, so an
        // all-caps-only random string must not start parsing. Nothing is stripped
        // from these, so ConsoleTitleParser returns null and the strict fallback
        // is the only shape that ever sees them.
        [TestCase("ABCDEFGHIJKLMNOP")]
        [TestCase("QWERTYUIOP ASDFGH")]
        [TestCase("XJKQ ZWVB PLMN")]
        [TestCase("AB CD EF")]
        public void should_not_start_parsing_an_all_caps_only_random_string(string postTitle)
        {
            Parser.Parser.ParseGameTitle(postTitle).Should().BeNull();
        }

        // REGRESSION GUARD: hashed / obfuscated names stay rejected.
        [TestCase("0e895c37245186812cb08aab1529cf8ee")]
        [TestCase("Backup_slsgdgfcs")]
        [TestCase("abcdefghijklmnopqrstuvwx")]
        [TestCase("ABCDEFGHIJK123")]
        public void should_keep_rejecting_a_hashed_release(string postTitle)
        {
            Parser.Parser.ParseGameTitle(postTitle).Should().BeNull();
        }

        // REGRESSION GUARD: PS4/PS5 title-id names keep being rejected.
        [TestCase("PPSA01234")]
        [TestCase("CUSA12345")]
        [TestCase("PPSA01234 PPSA01235")]
        public void should_keep_rejecting_a_playstation_title_id_name(string postTitle)
        {
            Parser.Parser.ParseGameTitle(postTitle).Should().BeNull();
        }

        // REGRESSION GUARD (#206): more words parsing must not make a LONGER
        // title match onto a shorter game. The parsed title keeps every word it
        // was given, so a title-prefix match still has a remainder to reject.
        [Test]
        public void should_not_shorten_a_longer_title_onto_a_shorter_game()
        {
            Parser.Parser.ParseGameTitle("ABCD Aaaaa Bbbbb Ccccc NSW-GROUPX")
                .GameTitle.Should().Be("ABCD Aaaaa Bbbbb Ccccc");

            Parser.Parser.ParseGameTitle("ABCD Aaaaa NSW-GROUPX")
                .GameTitle.Should().Be("ABCD Aaaaa");
        }

        // The permissive class is only ever offered on a normalised title, and
        // ConsoleTitleParser is what proves the title was normalised.
        [TestCase("ABCD Aaaaa Bbbbb Ccccc NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc")]
        [TestCase("ABCD Aaaaa Bbbbb Ccccc EUR NSW-GROUPX", "ABCD Aaaaa Bbbbb Ccccc")]
        public void should_hand_the_bare_all_caps_title_back_from_the_region_strip(string postTitle, string expected)
        {
            ConsoleTitleParser.RemoveRegionMetadata(postTitle).Should().Be(expected);
        }
    }
}
