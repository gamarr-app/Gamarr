using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    /// <summary>
    /// Base-game console releases carry their region and platform tags between
    /// the game name and the group - "Game.Name.EUR.NSW-GRP" - and those names
    /// PARSE, just with "EUR NSW" still glued onto the game title, so they then
    /// match no game at all and come back "Unknown Game" (#209). Only the
    /// Update releases of the same games matched, because the Update regex cuts
    /// the title at the marker and never sees the tags.
    ///
    /// The region tags were nowhere in the parser's vocabulary, which is also
    /// why "Game Name JPN NSW-GRP" did not parse at ALL: the console retry
    /// stripped "NSW-GRP" and handed back "Game Name JPN", whose trailing
    /// all-caps word the bare-title fallback rejects.
    /// </summary>
    [TestFixture]
    public class RegionTokenTitleFixture : CoreTest
    {
        // Dotted and spaced forms of the same name, with and without a region.
        [TestCase("Pikmin.4.EUR.NSW-VENOM")]
        [TestCase("Pikmin.4.USA.NSW-VENOM")]
        [TestCase("Pikmin.4.USA.PROPER.NSW-VENOM")]
        [TestCase("Pikmin.4.JPN.NSW-VENOM")]
        [TestCase("Pikmin.4.ASIA.NSW-VENOM")]
        [TestCase("Pikmin.4.NSW-VENOM")]
        [TestCase("Pikmin 4 EUR NSW-VENOM")]
        [TestCase("Pikmin 4 JPN NSW-VENOM")]
        [TestCase("Pikmin 4 USA NSW-LiGHTFORCE")]
        [TestCase("Pikmin 4 KOR NSW-VENOM")]
        [TestCase("Pikmin 4 EUR NSW")]
        public void should_parse_a_console_base_release_to_the_bare_game_title(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be("Pikmin 4");
        }

        // The year must not be invented out of a region tag either: these names
        // carry no year at all, and a non-zero one would make the title+year
        // lookup miss exactly as it did before.
        [TestCase("Pikmin.4.EUR.NSW-VENOM")]
        [TestCase("Pikmin 4 JPN NSW-VENOM")]
        public void should_not_read_a_year_off_a_console_base_release(string postTitle)
        {
            Parser.Parser.ParseGameTitle(postTitle).Year.Should().Be(0);
        }

        // The platform, group and language still come off the FULL release name,
        // which is the part that was removed to read the title.
        [TestCase("Pikmin.4.EUR.NSW-VENOM")]
        [TestCase("Pikmin 4 JPN NSW-VENOM")]
        public void should_still_detect_the_platform_from_the_removed_tags(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Platform.Should().Be(PlatformFamily.NintendoSwitch);
            parsed.PlatformString.Should().Be("Switch");
            parsed.ReleaseGroup.Should().Be("VENOM");
        }

        // JPN was the one region code no part of the parser knew - KOR, JAP and
        // RUS were all already language markers, JPN only in its bracketed form.
        [TestCase("Pikmin 4 JPN NSW-VENOM")]
        [TestCase("Pikmin.4.JPN.NSW-VENOM")]
        [TestCase("Pikmin 4 [JPN]")]
        public void should_read_japanese_from_a_bare_jpn_token(string postTitle)
        {
            LanguageParser.ParseLanguages(postTitle).Should().Contain(Language.Japanese);
        }

        // The Update releases of the same games always worked; they must keep
        // working, now that the tags are removed before the Update regex runs.
        [TestCase("Pikmin 4 Update v5.0.0 EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Update 4.2.0 USA NSW-VENOM")]
        [TestCase("Pikmin.4.Update.v5.0.0.EUR.NSW-VENOM")]
        public void should_keep_parsing_an_update_release_of_the_same_game(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.GameTitle.Should().Be("Pikmin 4");
            parsed.ContentType.Should().Be(ReleaseContentType.UpdateOnly);
        }

        // NSW is overwhelmingly "New South Wales" in real release names, so it
        // is never trusted as a bare word: it only counts when everything
        // between it and the end of the name is metadata too. None of these
        // qualify, so the name is handed on untouched.
        [TestCase("Highway Patrol NSW Australia Simulator")]
        [TestCase("NSW Road Rules Quiz")]
        [TestCase("Train Sim World NSW Sydney Trains Route")]
        [TestCase("Bushfire Rescue NSW Rural Fire Service")]
        public void should_not_treat_a_mid_name_nsw_as_a_platform_tag(string postTitle)
        {
            ConsoleTitleParser.RemoveRegionMetadata(postTitle).Should().BeNull();
        }

        // Same discriminator, the other way round: a region code is only a
        // region code in upper case. "Asia" and "Pal" start ordinary words.
        [TestCase("Lost Ark Asia")]
        [TestCase("Palworld")]
        [TestCase("Pikmin 4")]
        [TestCase("Hytale")]
        public void should_not_treat_a_title_cased_word_as_a_region_tag(string postTitle)
        {
            ConsoleTitleParser.RemoveRegionMetadata(postTitle).Should().BeNull();
        }

        // A dot-separated name needs its group put back: with no trailing group
        // it matches none of the title regexes and the whole parse returns null.
        // A space-separated one is read by the bare-title fallback instead, and
        // only without a group.
        [TestCase("Pikmin.4.EUR.NSW-VENOM", "Pikmin.4-VENOM")]
        [TestCase("Pikmin.4.NSW-VENOM", "Pikmin.4-VENOM")]
        [TestCase("Pikmin 4 EUR NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 EUR NSW", "Pikmin 4")]
        public void should_keep_the_group_only_on_a_dot_separated_name(string postTitle, string expected)
        {
            ConsoleTitleParser.RemoveRegionMetadata(postTitle).Should().Be(expected);
        }

        // Removing the tags must not be able to eat the whole name.
        [TestCase("EUR.NSW-VENOM")]
        [TestCase("NSW-VENOM")]
        [TestCase("JPN")]
        public void should_not_reduce_a_tag_only_name_to_nothing(string postTitle)
        {
            var cleaned = ConsoleTitleParser.RemoveRegionMetadata(postTitle);

            if (cleaned != null)
            {
                cleaned.Should().NotBeEmpty();
            }
        }
    }
}
