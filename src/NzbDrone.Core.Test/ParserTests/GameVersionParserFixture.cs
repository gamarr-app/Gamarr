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

        // ====== REAL-WORLD VERSION PARSING TESTS ======
        // Note: The parser regex expects dots between version components (v1.2.3), not spaces (v1 2 3)
        // Many real-world scraped titles have spaces, which don't match the current regex

        // Versions with proper dot separation (these should parse correctly)
        [TestCase("Hollow Knight Silksong v1.0.28324 MULTi10 FitGirl Repack", 1, 0, 28324, 0)]
        [TestCase("Baldurs Gate 3 v4.1.1.5009956-EMPRESS", 4, 1, 1, 5009956)]
        [TestCase("Red Dead Redemption 2 Ultimate Edition v1491.50-FitGirl", 1491, 50, 0, 0)]
        [TestCase("FIFA 23 v 1.0.82.43747 2022 PC RePack-FitGirl", 1, 0, 82, 43747)]
        [TestCase("FIFA 22 v 1.0.77.45722 2021 PC RePack-FitGirl", 1, 0, 77, 45722)]
        [TestCase("God of War v 1.0.8 1.0.447.8 2022 PC RePack от R.G. Механики", 1, 0, 8, 0)]
        [TestCase("God of War Ragnarök Digital Deluxe Edition v 1.0.668.5700 9.1 DLCs 2024 PC RePack-Wanterlude", 1, 0, 668, 5700)]
        [TestCase("Cyberpunk 2077 v2.1-CODEX", 2, 1, 0, 0)]
        [TestCase("Elden Ring v1.10-FitGirl", 1, 10, 0, 0)]
        [TestCase("Elden Ring Deluxe Edition v 1.03.2 DLC 2022 PC Steam-Rip", 1, 3, 2, 0)]
        [TestCase("Hollow Knight v 1.2.2.1 2 DLC 2017 PC RePack от xatab", 1, 2, 2, 1)]
        [TestCase("Hollow Knight v 1.5.78.11833 2017 PC RePack-Chovka", 1, 5, 78, 11833)]
        [TestCase("Starfield v1.11.36-RUNE", 1, 11, 36, 0)]
        [TestCase("Starfield v 1.8.88.0 DLCs 2023 PC RePack от Chovka", 1, 8, 88, 0)]
        [TestCase("Call of Duty Modern Warfare II v 9.7 2022 PC RePack от Decepticon", 9, 7, 0, 0)]
        [TestCase("Call of Duty Black Ops 6 v 11.1 DLC's 2024 PC RePack-FitGirl", 11, 1, 0, 0)]
        [TestCase("Horizon Zero Dawn Complete Edition v 1.0.11.14 DLCs 2020 PC RePack-Wanterlude", 1, 0, 11, 14)]
        public void should_parse_real_world_dotted_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // Releases without version numbers should return empty
        [TestCase("Hogwarts Legacy Deluxe Edition EMPRESS")]
        [TestCase("Starfield RUNE")]
        [TestCase("God of War-FLT")]
        [TestCase("Death Stranding-CPY")]
        [TestCase("Horizon Zero Dawn-CODEX")]
        [TestCase("Hades II RUNE")]
        public void should_return_empty_for_scene_releases_without_version(string title)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeFalse();
        }

        // Space-separated versions (like "v1 12 1") - parser now handles these
        [TestCase("God of War v1 0 1 Day 1 Patch FitGirl Repack", 1, 0, 1, 0)]
        [TestCase("Starfield v1 7 23 0 2 DLCs FitGirl Repack", 1, 7, 23, 0)]
        [TestCase("Baldurs Gate 3 Update v4 1 1 6848561 RUNE", 4, 1, 1, 6848561)]
        [TestCase("Cyberpunk 2077 Update v2 3", 2, 3, 0, 0)]
        public void should_parse_space_separated_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // Complex titles with multiple version patterns - parser takes the first valid match
        [TestCase("ELDEN RING Shadow of the Erdtree v1 12 v1 12 1 9 DLCs FitGirl Repack", 1, 12, 0, 0)]
        public void should_parse_first_version_in_complex_titles(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }
    }
}
