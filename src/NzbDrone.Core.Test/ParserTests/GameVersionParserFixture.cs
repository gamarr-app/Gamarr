using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class GameVersionParserFixture : CoreTest
    {
        [TestCase("Cyberpunk.2077.v1.5-CODEX", 1, 5, 0, 0)]
        [TestCase("Cyberpunk.2077.v2.1-CODEX", 2, 1, 0, 0)]
        [TestCase("Elden.Ring.v1.10.1-CODEX", 1, 10, 1, 0)]
        [TestCase("Baldurs.Gate.3.v4.1.1.3956366-GOG", 4, 1, 1, 3956366)]
        [TestCase("The.Witcher.3.Wild.Hunt.v1.32-GOG", 1, 32, 0, 0)]
        [TestCase("Red.Dead.Redemption.2.v1.0.1436.28-EMPRESS", 1, 0, 1436, 28)]
        [TestCase("Starfield.v1.11.36-RUNE", 1, 11, 36, 0)]
        [TestCase("Game-v1.0.0-RELOADED", 1, 0, 0, 0)]
        [TestCase("Game_v2.5_REPACK", 2, 5, 0, 0)]
        [TestCase("Game-1.2.3-SKIDROW", 1, 2, 3, 0)]
        public void should_parse_version_from_release_title(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        [TestCase("Game.Name.Build.12345-PLAZA", 0, 0, 0, 12345)]
        [TestCase("Game.Name.B12345-CODEX", 0, 0, 0, 12345)]
        public void should_parse_build_number_from_release_title(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        [TestCase("Game.Update.5-CODEX", 5, 0, 0, 0)]
        [TestCase("Game.Update.v1.2-SKIDROW", 1, 2, 0, 0)]
        [TestCase("Game.Patch.3.1-PLAZA", 3, 1, 0, 0)]
        public void should_parse_update_patch_version(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        [TestCase("Game.Name-CODEX")]
        [TestCase("Game.Name.2023-GOG")]
        [TestCase("Game.Name.REPACK-FitGirl")]
        public void should_return_empty_version_when_no_version_found(string title)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeFalse();
        }

        [TestCase("")]
        [TestCase(null)]
        public void should_handle_null_and_empty_input(string title)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeFalse();
        }
    }
}
