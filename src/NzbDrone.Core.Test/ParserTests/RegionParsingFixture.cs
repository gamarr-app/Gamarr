using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    // ConsoleTitleParser.ParseRegions backs the edition check in #213: an update
    // or DLC built for one regional edition cannot be applied to another.
    [TestFixture]
    public class RegionParsingFixture : CoreTest
    {
        [TestCase("Aaaaaaaa.2.EUR.NSW-GROUP", "EUR")]
        [TestCase("Aaaaaaaa 2 Update v5.5.2 USA NSW-GROUP", "USA")]
        [TestCase("Aaaaaaaa.2.Update.v5.5.2.JPN.NSW-GROUP", "JPN")]
        [TestCase("Pikmin.4.EUR.NSW-VENOM", "EUR")]
        [TestCase("Pikmin 4 EUR NSW-VENOM", "EUR")]
        [TestCase("Game.Name.ASIA.NSW-GRP", "ASIA")]
        [TestCase("Game.Name.KOR.NSW-GRP", "KOR")]
        public void should_read_the_region_from_a_scene_name(string title, string expected)
        {
            ConsoleTitleParser.ParseRegions(title).Should().BeEquivalentTo(new[] { expected });
        }

        [TestCase("PAL", "EUR")]
        [TestCase("EUR", "EUR")]
        [TestCase("JAP", "JPN")]
        [TestCase("JPN", "JPN")]
        [TestCase("NTSC-U", "USA")]
        [TestCase("NTSC-J", "JPN")]
        [TestCase("NTSC-C", "CHN")]
        public void should_fold_region_aliases_onto_one_code(string token, string expected)
        {
            // PAL and EUR are the same edition, as are JAP/JPN and NTSC-U/USA.
            // Keeping them apart would reject a legitimate add-on.
            ConsoleTitleParser.NormalizeRegion(token).Should().Be(expected);
        }

        [TestCase("NSW")]
        [TestCase("NSP")]
        [TestCase("NSZ")]
        [TestCase("XCI")]
        [TestCase("WUX")]
        [TestCase("WUD")]
        [TestCase("CIA")]
        public void should_not_treat_a_platform_or_container_token_as_a_region(string token)
        {
            // These share RegionPlatformToken with the real region codes but say
            // nothing about the edition.
            ConsoleTitleParser.NormalizeRegion(token).Should().BeNull();
        }

        [Test]
        public void should_not_guess_an_edition_from_a_bare_ntsc()
        {
            // NTSC covers USA, JPN and more, so mapping it anywhere invents a
            // mismatch.
            ConsoleTitleParser.NormalizeRegion("NTSC").Should().BeNull();
        }

        [TestCase("Game.Name.NSW-VENOM")]
        [TestCase("Hades.The.Blood.Price.DLC-GRP")]
        [TestCase("Super Mario Odyssey")]
        [TestCase("Highway Patrol NSW Australia Simulator")]
        [TestCase("")]
        [TestCase(null)]
        public void should_report_no_region_when_the_name_states_none(string title)
        {
            ConsoleTitleParser.ParseRegions(title).Should().BeEmpty();
        }

        [Test]
        public void should_not_read_a_lower_case_title_word_as_a_region()
        {
            ConsoleTitleParser.NormalizeRegion("Asia").Should().BeNull();
            ConsoleTitleParser.NormalizeRegion("Pal").Should().BeNull();
        }

        [Test]
        public void should_read_every_region_of_a_multi_region_dump()
        {
            var regions = ConsoleTitleParser.ParseRegions("Game.Name.USA.EUR.NSW-GRP");

            regions.Should().BeEquivalentTo(new[] { "USA", "EUR" });
        }

        [Test]
        public void should_read_a_region_that_carries_the_group_suffix()
        {
            ConsoleTitleParser.ParseRegions("Game.Name.EUR-VENOM").Should().BeEquivalentTo(new[] { "EUR" });
        }

        [Test]
        public void should_keep_removing_region_metadata_from_titles()
        {
            // ParseRegions must not disturb the stripping behaviour of #209.
            ConsoleTitleParser.RemoveRegionMetadata("Pikmin.4.EUR.NSW-VENOM").Should().Be("Pikmin.4-VENOM");
            ConsoleTitleParser.RemoveRegionMetadata("Pikmin 4 EUR NSW-VENOM").Should().Be("Pikmin 4");
        }

        [Test]
        public void should_return_an_ordinal_set_so_region_comparison_is_case_exact()
        {
            ConsoleTitleParser.ParseRegions("Game.Name.EUR.NSW-GRP").Contains("eur").Should().BeFalse();
            ConsoleTitleParser.ParseRegions("Game.Name.EUR.NSW-GRP").Single().Should().Be("EUR");
        }
    }
}
