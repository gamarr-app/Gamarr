using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class QualityParserFixture : CoreTest
    {
        [SetUp]
        public void Setup()
        {
        }

        // Scene releases
        [TestCase("Game.Name-CODEX")]
        [TestCase("Game.Name-PLAZA")]
        [TestCase("Game.Name-SKIDROW")]
        [TestCase("Game.Name-CPY")]
        [TestCase("Game.Name-EMPRESS")]
        [TestCase("Game.Name-FLT")]
        [TestCase("Game.Name-HOODLUM")]
        [TestCase("Game.Name-RELOADED")]
        [TestCase("Game.Name-TiNYiSO")]
        public void should_parse_scene_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Scene cracked releases
        [TestCase("Game.Name-CRACKED")]
        [TestCase("Game.Name.CRACK.ONLY-CODEX")]
        [TestCase("Game.Name.NO.DRM-GROUP")]
        public void should_parse_scene_cracked_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.SceneCracked);
        }

        // GOG releases (DRM-free)
        [TestCase("Game.Name.GOG")]
        [TestCase("Game.Name-GOG")]
        [TestCase("Game.Name.GOG.RIP")]
        public void should_parse_gog_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.GOG);
        }

        // Repack releases
        [TestCase("Game.Name-FitGirl.Repack")]
        [TestCase("Game.Name.FitGirl")]
        [TestCase("Game.Name-DODI")]
        [TestCase("Game.Name.DODI.Repack")]
        [TestCase("Game.Name-XATAB")]
        [TestCase("Game.Name.ElAmigos")]
        [TestCase("Game.Name.R.G.Mechanics")]
        [TestCase("Game.Name.REPACK")]
        public void should_parse_repack_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Repack with all DLC
        [TestCase("Game.Name.Incl.All.DLC-FitGirl")]
        [TestCase("Game.Name.Complete.Edition-DODI")]
        [TestCase("Game.Name.GOTY-Repack")]
        [TestCase("Game.Name.Ultimate.Edition-FitGirl")]
        public void should_parse_repack_all_dlc_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Steam releases
        [TestCase("Game.Name.Steam.Rip")]
        [TestCase("Game.Name.SteamRip")]
        [TestCase("Game.Name.STEAM.UNLOCKED")]
        public void should_parse_steam_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Steam);
        }

        // Portable releases
        [TestCase("Game.Name.Portable")]
        [TestCase("Game.Name.No.Install")]
        public void should_parse_portable_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Portable);
        }

        // ISO releases
        [TestCase("Game.Name.ISO")]
        [TestCase("Game.Name.Disc.Image")]
        public void should_parse_iso_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.ISO);
        }

        // Preload releases
        [TestCase("Game.Name.Preload")]
        [TestCase("Game.Name.Pre.Release")]
        public void should_parse_preload_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Preload);
        }

        // Update only releases
        [TestCase("Game.Name.Update.Only")]
        [TestCase("Game.Name.Patch.Only")]
        public void should_parse_update_only_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.UpdateOnly);
        }

        // Multi-language releases
        [TestCase("Game.Name.MULTI10")]
        [TestCase("Game.Name.MULTi9")]
        [TestCase("Game.Name.MULTiLANGUAGE")]
        public void should_parse_multilang_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.MultiLang);
        }

        // Unknown releases
        [TestCase("Game.Name.Unknown.Format")]
        [TestCase("Random.Title.2024")]
        public void should_parse_unknown_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Proper releases
        [TestCase("Game.Name.PROPER-CODEX")]
        public void should_parse_proper_release(string title)
        {
            var result = QualityParser.ParseQuality(title);
            result.Revision.Version.Should().Be(2);
        }
    }
}
