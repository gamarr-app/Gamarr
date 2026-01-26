using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class GameVersionParserFixture : CoreTest
    {
        // Basic dotted version parsing
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
        [TestCase("The Witness [v 1.0u4] (2016) PC | RePack", 1, 0, 0, 0)]
        [TestCase("Split.Fiction.v20250317.UPDATE-KaOs", 20250317, 0, 0, 0)]
        [TestCase("Game.v20240101-SKIDROW", 20240101, 0, 0, 0)]
        [TestCase("Hytale (v20260120)", 20260120, 0, 0, 0)]
        [TestCase("Hytale (v2026.01.20)", 2026, 1, 20, 0)]
        [TestCase("The Outer Worlds 2 Premium Edition (v1030 All DLCs Bonus", 1030, 0, 0, 0)]
        [TestCase("The Outer Worlds 2: Premium Edition (v.1.0.6.0) + 7 DLC [amd64] [Multi] [Wine]", 1, 0, 6, 0)]
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
        [TestCase("Split Fiction RUNE [build 18353366] [ALL DLCs] [Multi11]", 0, 0, 0, 18353366)]
        [TestCase("High On Life Build 10158842 MULTi5 REPACK KaOs", 0, 0, 0, 10158842)]
        [TestCase("High On Life DLC Bundle (Build 12321732 High On Knife DLC Windows 7 Fix MULTi5) [FitGirl Repack Selective Download from 35 2 GB]", 0, 0, 0, 12321732)]
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
        [TestCase("The Witness [Update 18] (2016) PC | RePack", 18, 0, 0, 0)]
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
        [TestCase("Game.Name.2023-GOG")] // Year should not be parsed as version
        [TestCase("Game.Name.REPACK-FitGirl")]
        [TestCase("Split Fiction (+ Online Multiplayer, MULTi11) [FitGirl Repack, Selective Download from 52.9 GB]")]
        [TestCase("Game Name [Repack from 10.5 GB]")]
        [TestCase("Game 56.7 GiB Download")]
        [TestCase("Game [100 MB]")]
        [TestCase("Borderlands 3 (2019) PC")] // "3" and "2019" are not versions
        [TestCase("Portal 2 Repack")] // "2" is part of title
        [TestCase("Hades II RUNE")] // "II" is Roman numeral, not version
        [TestCase("Game v5 Edition")] // "v5" alone has no minor version
        [TestCase("Game 2025 Edition")] // Year without v prefix
        [TestCase("FIFA 25 Repack")] // "25" is part of title
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

        // Versions with proper dot separation
        [TestCase("Hollow Knight Silksong v1.0.28324 MULTi10 FitGirl Repack", 1, 0, 28324, 0)]
        [TestCase("Baldurs Gate 3 v4.1.1.5009956-EMPRESS", 4, 1, 1, 5009956)]
        [TestCase("Red Dead Redemption 2 Ultimate Edition v1491.50-FitGirl", 1491, 50, 0, 0)]
        [TestCase("FIFA 23 v 1.0.82.43747 2022 PC RePack-FitGirl", 1, 0, 82, 43747)]
        [TestCase("FIFA 22 v 1.0.77.45722 2021 PC RePack-FitGirl", 1, 0, 77, 45722)]
        [TestCase("God of War v 1.0.8 1.0.447.8 2022 PC RePack от R.G. Механики", 1, 0, 8, 0)]
        [TestCase("God of War Ragnarök v 1.0.668.5700 9.1 DLCs 2024 PC RePack-Wanterlude", 1, 0, 668, 5700)]
        [TestCase("Cyberpunk 2077 v2.1-CODEX", 2, 1, 0, 0)]
        [TestCase("Elden Ring v1.10-FitGirl", 1, 10, 0, 0)]
        [TestCase("Elden Ring v 1.03.2 DLC 2022 PC Steam-Rip", 1, 3, 2, 0)]
        [TestCase("Hollow Knight v 1.2.2.1 2 DLC 2017 PC RePack от xatab", 1, 2, 2, 1)]
        [TestCase("Hollow Knight v 1.5.78.11833 2017 PC RePack-Chovka", 1, 5, 78, 11833)]
        [TestCase("Starfield v1.11.36-RUNE", 1, 11, 36, 0)]
        [TestCase("Starfield v 1.8.88.0 DLCs 2023 PC RePack от Chovka", 1, 8, 88, 0)]
        [TestCase("Call of Duty Modern Warfare II v 9.7 2022 PC RePack от Decepticon", 9, 7, 0, 0)]
        [TestCase("Call of Duty Black Ops 6 v 11.1 DLCs 2024 PC RePack-FitGirl", 11, 1, 0, 0)]
        [TestCase("Horizon Zero Dawn v 1.0.11.14 DLCs 2020 PC RePack-Wanterlude", 1, 0, 11, 14)]
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
        [TestCase("Black Myth Wukong Deluxe Edition-retvil")]
        [TestCase("Borderlands 3 (2019) PC | RePack от xatab")]
        [TestCase("Cuphead [DODI Repack]")]
        [TestCase("Assassins Creed Valhalla-EMPRESS")]
        public void should_return_empty_for_releases_without_version(string title)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeFalse();
        }

        // Space-separated versions (like "v1 12 1")
        [TestCase("God of War v1 0 1 Day 1 Patch FitGirl Repack", 1, 0, 1, 0)]
        [TestCase("Starfield v1 7 23 0 2 DLCs FitGirl Repack", 1, 7, 23, 0)]
        [TestCase("Baldurs Gate 3 Update v4 1 1 6848561 RUNE", 4, 1, 1, 6848561)]
        [TestCase("Cyberpunk 2077 Update v2 3", 2, 3, 0, 0)]
        [TestCase("ELDEN RING Shadow of the Erdtree Update v1 16-RUNE", 1, 16, 0, 0)]
        [TestCase("ELDEN RING Deluxe Edition Shadow of the Erdtree Premium Bundle (v1 16 All DLCs Bonus Content Online Multiplayer MULTi15)", 1, 16, 0, 0)]
        [TestCase("Manifold Garden Update v1 1 0 15463-CODEX", 1, 1, 0, 15463)]
        public void should_parse_space_separated_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // Complex titles with multiple version patterns
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

        // Lies of P versions
        [TestCase("Lies of P [v 1.5.0.0 Hotfix + DLCs] (2023) PC", 1, 5, 0, 0)]
        [TestCase("Lies.Of.P.Overture.v1.10.0.0.REPACK-KaOs", 1, 10, 0, 0)]
        [TestCase("Lies of P Overture Update v1 12 0 0-RUNE", 1, 12, 0, 0)]
        public void should_parse_lies_of_p_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // Yakuza versions with build numbers
        [TestCase("Yakuza 0 [v.1.15.9088 build 21476300] (2025) PC", 1, 15, 9088, 0)]
        [TestCase("Yakuza Kiwami 2 [v.2.11 build 20733812] (2025) PC", 2, 11, 0, 0)]
        public void should_parse_yakuza_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // Celeste versions - space delimiter works
        [TestCase("Celeste v1.4.0.0 [Build 6458966] Repack Team-LiL", 1, 4, 0, 0)]
        public void should_parse_celeste_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // Space-delimited versions without v prefix (jc141 style)
        [TestCase("FEZ 1.12 MULTi9 GNU/Linux Wine jc141 (Appid=224760)", 1, 12, 0, 0)]
        [TestCase("Limbo 1.3 MULTi18 GNU/Linux Wine jc141", 1, 3, 0, 0)]
        [TestCase("Braid 1.010 MULTi9 jc141", 1, 10, 0, 0)]
        [TestCase("Is this Game Trying to Kill Me? 1.0.11 MULTi7 GNU/Linux Wine jc141 (Appid=2658470)", 1, 0, 11, 0)]
        [TestCase("[DL] Is this Game Trying to Kill Me? [P] [RUS + ENG + 5] (2024, Adventure) (1.0.11) [Portable]", 1, 0, 11, 0)]
        [TestCase("Is this Game Trying to Kill Me 1 0 11 MULTi7 GNU Linux Wine", 1, 0, 11, 0)]
        public void should_parse_space_delimited_versions_without_v_prefix(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // Control versions - only dotted versions after proper delimiter work
        [TestCase("Control [v.0.0.518.2177 Build 17833993] (2020) PC", 0, 0, 518, 2177)]
        public void should_parse_control_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // FitGirl parenthesized format with comma after version
        [TestCase("Manifold Garden (v1.1.0.14651, MULTi14) [FitGirl Repack]", 1, 1, 0, 14651)]
        [TestCase("Celeste (v1.4.0.0, MULTi15) [FitGirl Repack]", 1, 4, 0, 0)]
        [TestCase("FIFA 22 (v1.0.77.45722, MULTi21) [FitGirl Monkey Repack]", 1, 0, 77, 45722)]
        [TestCase("Terra Invicta (v1.0.25, MULTi14) [FitGirl Repack]", 1, 0, 25, 0)]
        [TestCase("Wreckreation (v1.2.0.147169, MULTi12) [FitGirl Repack]", 1, 2, 0, 147169)]
        public void should_parse_fitgirl_comma_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // jc141 format without v prefix (4-part version space-delimited)
        [TestCase("Manifold Garden 1.1.0.17370 MULTi14 GNU/Linux Wine jc141", 1, 1, 0, 17370)]
        [TestCase("Game Name 1.2.3.4567 MULTi10", 1, 2, 3, 4567)]
        public void should_parse_space_delimited_four_part_versions(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // jc141 format with standalone build number before MULTi
        [TestCase("Hades II 1131346 MULTi15 GNU Linux Wine jc141", 0, 0, 0, 1131346)]
        [TestCase("Game Name 9876543 MULTi10", 0, 0, 0, 9876543)]
        public void should_parse_standalone_build_before_multi(string title, int major, int minor, int patch, int build)
        {
            var version = QualityParser.ParseGameVersion(title);

            version.HasValue.Should().BeTrue();
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
        }

        // Space-separated versions in parentheses (DODI/FitGirl format)
        [TestCase("Hades II (v1 131346 Bonus Content MULTi15) [DODI Repack]", 1, 131346, 0, 0)]
        [TestCase("Hades II Hades 2 (v1 131346 Bonus OST MULTi15) [FitGirl Repack]", 1, 131346, 0, 0)]
        [TestCase("Hades II Update v1 131641-RUNE", 1, 131641, 0, 0)]
        public void should_parse_space_separated_versions_in_parens(string title, int major, int minor, int patch, int build)
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
