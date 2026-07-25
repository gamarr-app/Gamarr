using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ContentTypeParserFixture : CoreTest
    {
        [TestCase("Game.Name.DLC.Only-CODEX", ReleaseContentType.DlcOnly)]
        [TestCase("Game.Name.DLC-PLAZA", ReleaseContentType.DlcOnly)]
        [TestCase("Game.Name.Addon.Only-SKIDROW", ReleaseContentType.DlcOnly)]
        public void should_detect_dlc_only_releases(string title, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(title).Should().Be(expected);
        }

        [TestCase("Game.Name.Update.Only-CODEX", ReleaseContentType.UpdateOnly)]
        [TestCase("Game.Name.Patch.Only-PLAZA", ReleaseContentType.UpdateOnly)]
        [TestCase("Game.Name.Update.5.Only-SKIDROW", ReleaseContentType.UpdateOnly)]
        [TestCase("Game.Name.Hotfix.Only-RELOADED", ReleaseContentType.UpdateOnly)]
        public void should_detect_update_only_releases(string title, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(title).Should().Be(expected);
        }

        // Real scene update names (srrdb corpus) carry a version, not "only"
        [TestCase("Wreckreation.Update.v1.7.0.153795-RUNE", ReleaseContentType.UpdateOnly)]
        [TestCase("The.Hundred.Line.Last.Defense.Academy.Update.v1.2.2-RUNE", ReleaseContentType.UpdateOnly)]
        [TestCase("Solo.Leveling.ARISE.OVERDRIVE.Update.v1.1.93-RUNE", ReleaseContentType.UpdateOnly)]
        [TestCase("7.Days.To.Die.Update.v2.5.b23.incl.DLC-RUNE", ReleaseContentType.UpdateOnly)]
        [TestCase("Game.Name.Patch.1.05-CODEX", ReleaseContentType.UpdateOnly)]
        [TestCase("Game.Name.Update.Build.14332-TENOKE", ReleaseContentType.UpdateOnly)]
        public void should_detect_versioned_update_releases(string title, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(title).Should().Be(expected);
        }

        // Games whose titles contain update-ish words must not classify as updates
        [TestCase("Patch.Quest-TENOKE")]
        [TestCase("Update.Eight.Deluxe.Edition-RUNE")]
        public void should_not_classify_titles_containing_update_words_without_version(string title)
        {
            QualityParser.ParseContentType(title).Should().Be(ReleaseContentType.Unknown);
        }

        [TestCase("Game.Name.Season.Pass-CODEX", ReleaseContentType.SeasonPass)]
        [TestCase("Game.Name.DLC.Bundle-PLAZA", ReleaseContentType.SeasonPass)]
        [TestCase("Game.Name.Expansion.Pass-SKIDROW", ReleaseContentType.SeasonPass)]
        [TestCase("Game.Name.Content.Pack-RELOADED", ReleaseContentType.SeasonPass)]
        public void should_detect_season_pass_releases(string title, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(title).Should().Be(expected);
        }

        [TestCase("Game.Name.Expansion-CODEX", ReleaseContentType.Expansion)]
        [TestCase("Game.Name.Expansion.Pack-PLAZA", ReleaseContentType.Expansion)]
        [TestCase("Game.Name.Standalone.Expansion-SKIDROW", ReleaseContentType.Expansion)]
        public void should_detect_expansion_releases(string title, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(title).Should().Be(expected);
        }

        [TestCase("Game.Name.Complete.Edition-CODEX", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.GOTY-PLAZA", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.Game.of.the.Year-SKIDROW", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.Ultimate.Edition-RELOADED", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.Definitive.Edition-GOG", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.Gold.Edition-EMPRESS", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.Legendary.Edition-CODEX", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.Includes.All.DLC-PLAZA", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.With.All.DLCs-SKIDROW", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.All.DLCs.Included-RELOADED", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.Premium.Edition-GOG", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.MULTi10.Repack.All.DLC-FAKE", ReleaseContentType.BaseGameWithAllDlc)]
        [TestCase("Game.Name.v2.1.All.DLC-EMPRESS", ReleaseContentType.BaseGameWithAllDlc)]
        public void should_detect_complete_edition_releases(string title, ReleaseContentType expected)
        {
            QualityParser.ParseContentType(title).Should().Be(expected);
        }

        [Test]
        public void all_dlc_bundle_should_remain_season_pass()
        {
            QualityParser.ParseContentType("Game.Name.All.DLC.Bundle-CODEX").Should().Be(ReleaseContentType.SeasonPass);
        }

        [TestCase("Game.Name-CODEX")]
        [TestCase("Game.Name.v1.5-PLAZA")]
        [TestCase("Game.Name.Repack-FitGirl")]
        [TestCase("Game.Name-GOG")]
        public void should_return_unknown_for_base_game_releases(string title)
        {
            QualityParser.ParseContentType(title).Should().Be(ReleaseContentType.Unknown);
        }

        [TestCase("")]
        [TestCase(null)]
        public void should_handle_null_and_empty_input(string title)
        {
            QualityParser.ParseContentType(title).Should().Be(ReleaseContentType.Unknown);
        }

        [Test]
        public void release_content_type_should_indicate_requires_base_game()
        {
            ReleaseContentType.DlcOnly.RequiresBaseGame().Should().BeTrue();
            ReleaseContentType.UpdateOnly.RequiresBaseGame().Should().BeTrue();
            ReleaseContentType.SeasonPass.RequiresBaseGame().Should().BeTrue();
            ReleaseContentType.BaseGame.RequiresBaseGame().Should().BeFalse();
            ReleaseContentType.BaseGameWithAllDlc.RequiresBaseGame().Should().BeFalse();
            ReleaseContentType.Expansion.RequiresBaseGame().Should().BeFalse();
            ReleaseContentType.Unknown.RequiresBaseGame().Should().BeFalse();
        }

        [Test]
        public void release_content_type_should_indicate_includes_base_game()
        {
            ReleaseContentType.BaseGame.IncludesBaseGame().Should().BeTrue();
            ReleaseContentType.BaseGameWithAllDlc.IncludesBaseGame().Should().BeTrue();
            ReleaseContentType.Unknown.IncludesBaseGame().Should().BeTrue();
            ReleaseContentType.DlcOnly.IncludesBaseGame().Should().BeFalse();
            ReleaseContentType.UpdateOnly.IncludesBaseGame().Should().BeFalse();
            ReleaseContentType.SeasonPass.IncludesBaseGame().Should().BeFalse();
            ReleaseContentType.Expansion.IncludesBaseGame().Should().BeFalse();
        }

        [Test]
        public void release_content_type_should_indicate_includes_dlc()
        {
            ReleaseContentType.BaseGameWithAllDlc.IncludesDlc().Should().BeTrue();
            ReleaseContentType.DlcOnly.IncludesDlc().Should().BeTrue();
            ReleaseContentType.SeasonPass.IncludesDlc().Should().BeTrue();
            ReleaseContentType.Expansion.IncludesDlc().Should().BeTrue();
            ReleaseContentType.BaseGame.IncludesDlc().Should().BeFalse();
            ReleaseContentType.UpdateOnly.IncludesDlc().Should().BeFalse();
            ReleaseContentType.Unknown.IncludesDlc().Should().BeFalse();
        }
    }
}
