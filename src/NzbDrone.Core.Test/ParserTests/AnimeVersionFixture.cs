using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class GameVersionFixture : CoreTest
    {
        // Test PROPER release detection for games
        [TestCase("Game.Title.2023-CODEX", 1)]
        [TestCase("Game.Title.PROPER.2023-CODEX", 2)]
        [TestCase("Game.Title.2023.PROPER-PLAZA", 2)]
        public void should_parse_version_from_proper(string title, int version)
        {
            var result = QualityParser.ParseQuality(title);
            result.Revision.Version.Should().Be(version);
        }

        // Test game version patterns don't affect revision
        [TestCase("Game.Title.v1.0.5-CODEX", 1)]
        [TestCase("Game.Title.Build.12345-CODEX", 1)]
        [TestCase("Game.Title.Update.1.2.3-CODEX", 1)]
        public void should_not_parse_game_version_as_revision(string title, int version)
        {
            var result = QualityParser.ParseQuality(title);
            result.Revision.Version.Should().Be(version);
        }
    }
}
