using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    /// <summary>
    /// A named DLC release never matched its game even with the game in the
    /// library, the DLC slot monitored and the region tag stripped (#212).
    /// Three independent defects had to line up for that:
    ///
    ///  1. Expansion was not base-requiring, so BOTH prefix paths in
    ///     ParsingService returned before the DLC name latitude in
    ///     IsAddonOfGameTitle could run.
    ///  2. The content-type classifier answered with the first content word in
    ///     the name, so a DLC whose OWN NAME contains "Expansion" lost to that
    ///     word despite carrying an explicit DLC marker.
    ///  3. The metadata tail after the region anchor accepted only all-caps
    ///     tokens, so a mixed-case scene tag ended the run, the anchored regex
    ///     matched a later region token instead, and the earlier tag stayed in
    ///     the title.
    /// </summary>
    [TestFixture]
    public class NamedDlcReleaseFixture : CoreTest
    {
        // --- Defect 2: an explicit DLC marker beats a content word in the name.

        // Every content-type word that turns up inside real DLC names, spaced
        // and dotted. Each of these is a DLC release OF an add-on whose name
        // happens to contain the word, not a release of that content type.
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Night Pass DLC EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Night Pack DLC EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Night Bundle DLC EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Night Edition DLC EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Night Collection DLC EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Night Complete DLC EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Night Deluxe DLC EUR NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Expansion.DLC.EUR.NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Pass.DLC.EUR.NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Pack.DLC.EUR.NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Bundle.DLC.EUR.NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Edition.DLC.EUR.NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Collection.DLC.EUR.NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Complete.DLC.EUR.NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Deluxe.DLC.EUR.NSW-VENOM")]
        public void should_let_an_explicit_dlc_marker_win_over_a_content_word_in_the_name(string postTitle)
        {
            QualityParser.ParseContentType(postTitle).Should().Be(ReleaseContentType.DlcOnly);
        }

        // The markers that are statements about the release rather than words in
        // an add-on's name keep their precedence over the DLC tier.
        [TestCase("Pikmin 4 Night Expansion Pass EUR NSW-VENOM", ReleaseContentType.SeasonPass)]
        [TestCase("Pikmin 4 Night Season Pass EUR NSW-VENOM", ReleaseContentType.SeasonPass)]
        [TestCase("Pikmin 4 Night Content Pack EUR NSW-VENOM", ReleaseContentType.SeasonPass)]
        [TestCase("Pikmin 4 Night Complete Edition EUR NSW-VENOM", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Pikmin 4 Update v2.5 incl DLC EUR NSW-VENOM", ReleaseContentType.UpdateOnly)]
        [TestCase("Pikmin.4.KIOSK.eShop.INTERNAL.NSW-VENOM", ReleaseContentType.Demo)]

        // With no marker at all the content word is all there is to go on, so
        // the expansion tier still answers.
        [TestCase("Pikmin 4 Night Expansion EUR NSW-VENOM", ReleaseContentType.Expansion)]
        [TestCase("Pikmin 4 Night Expansion Pack EUR NSW-VENOM", ReleaseContentType.Expansion)]
        public void should_keep_the_precedence_of_the_tiers_above_dlc(string postTitle, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(postTitle).Should().Be(expected);
        }

        // Found by the #212 audit, same defect as above read the other way: an
        // explicit marker was being ignored rather than outranked.
        //
        // "DLC Pack" is several DLCs at once and belongs with "DLC Bundle" and
        // "DLC Unlocker". It used to come back Unknown, which IncludesBaseGame()
        // reads as "assume base game", so the release was never base-requiring
        // and no prefix path looked for its game.
        [TestCase("Pikmin 4 DLC Pack 2 EUR MULTi5 NSW-VENOM", ReleaseContentType.SeasonPass)]
        [TestCase("Pikmin 4 DLC Pack EUR NSW-VENOM", ReleaseContentType.SeasonPass)]

        // And "Incl DLC" is a base game WITH its DLC, not a DLC-only release.
        // Only "Incl ALL DLC" was recognised, so the short form classified as
        // DLC-only and would have imported into the DLC subfolder instead of as
        // the game itself.
        [TestCase("Pikmin 4 Incl DLC EUR NSW-VENOM", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Pikmin 4 Includes DLC EUR NSW-VENOM", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Pikmin 4 With DLC EUR NSW-VENOM", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Pikmin 4 Incl All DLC EUR NSW-VENOM", ReleaseContentType.BaseGameWithAllDlc)]
        public void should_classify_the_shapes_found_by_the_audit(string postTitle, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(postTitle).Should().Be(expected);
        }

        // --- Defect 3: mixed-case scene tags in the metadata tail.

        [TestCase("Pikmin 4 DLC Pack 2 EUR MULTi5 NSW-VENOM", "Pikmin 4 DLC Pack 2")]
        [TestCase("Pikmin 4 CHN READNFO iNTERNAL NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 EUR MULTi NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 EUR MULTi5 NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 EUR eShop iNTERNAL NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 EUR PROPER REPACK NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 JPN MULTi9 READNFO NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin.4.EUR.MULTi5.iNTERNAL.NSW-VENOM", "Pikmin.4-VENOM")]
        public void should_strip_a_region_tag_through_a_mixed_case_scene_tail(string postTitle, string expected)
        {
            ConsoleTitleParser.RemoveRegionMetadata(postTitle).Should().Be(expected);
        }

        // Broadening the tail must not let the strip take away the NSP/NSZ/XCI
        // token that the title array's "Nintendo Switch <Title> NSP RUS Multi10"
        // shape is TERMINATED by: the shape would stop matching, the bare-title
        // fallback would read the name instead, and the platform prefix would
        // end up inside the parsed title.
        [TestCase("Nintendo Switch Hollow Knight NSZ RUS Multi9", "Hollow Knight")]
        [TestCase("Nintendo Switch Pikmin 4 NSP RUS Multi10", "Pikmin 4")]
        public void should_not_strip_the_dump_token_a_platform_prefixed_title_needs(string postTitle, string expected)
        {
            ConsoleTitleParser.RemoveRegionMetadata(postTitle).Should().BeNull();
            Parser.Parser.ParseGameTitle(postTitle).PrimaryGameTitle.Should().Be(expected);
        }

        // ...and the veto is limited to those three tokens, so an ordinary
        // region tag on a game whose name really does start with the platform
        // words is still stripped.
        [TestCase("Nintendo Switch Sports EUR NSW-VENOM", "Nintendo Switch Sports")]
        public void should_still_strip_a_region_tag_from_a_game_named_after_the_platform(string postTitle, string expected)
        {
            ConsoleTitleParser.RemoveRegionMetadata(postTitle).Should().Be(expected);
            Parser.Parser.ParseGameTitle(postTitle).PrimaryGameTitle.Should().Be(expected);
        }

        // --- Defect 1: the reported release end to end.

        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM")]
        [TestCase("Pikmin.4.Night.Expansion.DLC.EUR.NSW-VENOM")]
        [TestCase("Pikmin 4 Night Expansion DLC NSW-VENOM")]
        public void should_parse_the_reported_release_as_a_base_requiring_dlc(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.ContentType.Should().Be(ReleaseContentType.DlcOnly);
            parsed.ContentType.RequiresBaseGame().Should().BeTrue();
            parsed.ContentType.OccupiesDlcSlot().Should().BeTrue();
            parsed.PrimaryGameTitle.Should().Be("Pikmin 4 Night Expansion DLC");
        }

        // An expansion with no explicit marker is base-requiring too, which is
        // what lets the prefix paths find its game at all.
        [TestCase("Pikmin 4 Night Expansion EUR NSW-VENOM")]
        [TestCase("Pikmin 4 Night Expansion Pack EUR NSW-VENOM")]
        public void should_treat_an_expansion_as_base_requiring(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.ContentType.Should().Be(ReleaseContentType.Expansion);
            parsed.ContentType.RequiresBaseGame().Should().BeTrue();
            parsed.ContentType.OccupiesDlcSlot().Should().BeTrue();
        }

        // --- Expected behaviour 2: the release remainder maps to the slot.

        // The slot's whole title appears verbatim in the release name, which the
        // containment test already handled.
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Pikmin 4: Night Expansion")]
        [TestCase("Pikmin.4.Night.Expansion.DLC.EUR.NSW-VENOM", "Pikmin 4: Night Expansion")]
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Pikmin 4 - Night Expansion")]

        // ...and the cases it did not: the slot spells the shared game prefix or
        // the add-on's articles differently from the release.
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Pikmin 4: The Night Expansion")]
        [TestCase("Pikmin 4 The Night Expansion DLC EUR NSW-VENOM", "Pikmin 4: Night Expansion")]
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Pikmin IV: Night Expansion")]
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Night Expansion")]
        public void should_map_a_release_remainder_to_the_matching_dlc_slot(string releaseTitle, string slotTitle)
        {
            GameComponentMatcher.ReleaseMatchesDlcTitle(releaseTitle, slotTitle, "Pikmin 4").Should().BeTrue();
        }

        // The remainder comparison must not map a release onto the wrong slot of
        // the same game, which is the whole risk of comparing less than the
        // full title.
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Pikmin 4: Day Expansion")]
        [TestCase("Pikmin 4 Wave 2 DLC EUR NSW-VENOM", "Pikmin 4: Wave 1")]
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Pikmin 4: Night Expansion Deluxe Upgrade")]
        public void should_not_map_a_release_onto_a_different_slot_of_the_same_game(string releaseTitle, string slotTitle)
        {
            GameComponentMatcher.ReleaseMatchesDlcTitle(releaseTitle, slotTitle, "Pikmin 4").Should().BeFalse();
        }

        // A slot name too short to be evidence of anything is not matched on.
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Pikmin 4: DX")]
        [TestCase("Pikmin 4 Night Expansion DLC EUR NSW-VENOM", "Pikmin 4: II")]
        public void should_not_map_on_a_too_short_slot_name(string releaseTitle, string slotTitle)
        {
            GameComponentMatcher.ReleaseMatchesDlcTitle(releaseTitle, slotTitle, "Pikmin 4").Should().BeFalse();
        }

        // --- Regressions the #212 fix had to keep green.

        // #206: an UPDATE gets no name latitude - "<Game> <Word> Update vX" is
        // an update of "<Game> <Word>", a separate IGDB entry.
        [TestCase("Pikmin 4 Deluxe NSW-VENOM", ReleaseContentType.Unknown)]
        [TestCase("Pikmin 4 Deluxe Update v1.2.0 NSW-VENOM", ReleaseContentType.UpdateOnly)]

        // #211
        [TestCase("Pikmin 4 Wave 2 Night DLC NSW-VENOM", ReleaseContentType.DlcOnly)]
        [TestCase("Pikmin 4 Update v11.2.0 NSW-VENOM", ReleaseContentType.UpdateOnly)]

        // #203/#204: a bundle stays a bundle.
        [TestCase("Pikmin 4 Wave 1-6 DLC Unlocker NSW-VENOM", ReleaseContentType.SeasonPass)]
        [TestCase("Pikmin 4 DLC Unlocker EUR NSW-VENOM", ReleaseContentType.SeasonPass)]

        // #207: the demo tier shares its tail vocabulary with the region strip
        // now, so it has to keep answering first.
        [TestCase("Pikmin.4.KIOSK.eShop.INTERNAL.NSW-VENOM", ReleaseContentType.Demo)]
        [TestCase("Pikmin 4 KIOSK eShop iNTERNAL NSW-VENOM", ReleaseContentType.Demo)]
        [TestCase("Pikmin 4 EUR DEMO NSW-VENOM", ReleaseContentType.Demo)]
        public void should_keep_the_already_fixed_content_types_green(string postTitle, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(postTitle).Should().Be(expected);
        }

        // #209 titles must still reduce to the bare game name, and #208's
        // abbreviation dot must survive the broader tail.
        [TestCase("Pikmin.4.EUR.NSW-VENOM", "Pikmin 4")]
        [TestCase("Pikmin 4 JPN NSW-VENOM", "Pikmin 4")]
        [TestCase("Aaa Bbbbb Ccccc Dddd. E Ffffff NSW-VENOM", "Aaa Bbbbb Ccccc Dddd. E Ffffff")]
        public void should_keep_the_already_fixed_titles_green(string postTitle, string expected)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle);

            parsed.Should().NotBeNull();
            parsed.PrimaryGameTitle.Should().Be(expected);
        }

        // NSW is overwhelmingly "New South Wales", and the broader tail must not
        // have loosened the anchoring that keeps it from being read as a tag.
        [TestCase("Highway Patrol NSW Australia Simulator")]
        [TestCase("Train Sim World NSW Sydney Trains Route")]
        public void should_still_refuse_a_mid_name_nsw(string postTitle)
        {
            ConsoleTitleParser.RemoveRegionMetadata(postTitle).Should().BeNull();
        }
    }
}
