using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ExtendedQualityParserRegex : CoreTest
    {
        [SetUp]
        public void Setup()
        {
        }

        // Test REAL modifier detection for game releases
        [TestCase("Game.Title-CODEX", 0)]
        [TestCase("Game.Title.REAL.PROPER-CODEX", 1)]
        [TestCase("Game.Title.REAL.REAL.PROPER-PLAZA", 2)]
        [TestCase("Game.Title.PROPER.FIX-SKIDROW", 1)]
        public void should_parse_reality_from_title(string title, int reality)
        {
            QualityParser.ParseQuality(title).Revision.Real.Should().Be(reality);
        }

        // Test PROPER/version detection for game releases
        [TestCase("Game.Title-CODEX", 1)]
        [TestCase("Game.Title.PROPER-CODEX", 2)]
        [TestCase("Game.Title.REAL.PROPER-PLAZA", 2)]
        [TestCase("Game.Title.v1.05.Update-CODEX", 1)]
        public void should_parse_version_from_title(string title, int version)
        {
            QualityParser.ParseQuality(title).Revision.Version.Should().Be(version);
        }

        // Test scene group detection
        [TestCase("Game.Title-CODEX")]
        [TestCase("Game.Title-PLAZA")]
        [TestCase("Game.Title-SKIDROW")]
        [TestCase("Game.Title-CPY")]
        [TestCase("Game.Title-EMPRESS")]
        [TestCase("Game.Title-RELOADED")]
        [TestCase("Game.Title-TiNYiSO")]
        public void should_parse_scene_release(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Test repack group detection
        [TestCase("Game.Title-FitGirl")]
        [TestCase("Game.Title-DODI")]
        [TestCase("Game.Title.REPACK")]
        [TestCase("Game.Title-XATAB")]
        public void should_parse_repack_release(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Test GOG detection
        [TestCase("Game.Title.GOG")]
        [TestCase("Game.Title-GOG")]
        [TestCase("Game.Title.GOG.RIP")]
        public void should_parse_gog_release(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.GOG);
        }
    }
}
