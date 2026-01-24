using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class ReleaseStructureValidatorFixture : CoreTest<ReleaseStructureValidator>
    {
        private const string TestPath = "/games/test-release";

        private void GivenFolderExists(bool exists = true)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.FolderExists(TestPath))
                  .Returns(exists);
        }

        private void GivenFiles(params string[] relativeFiles)
        {
            var fullPaths = relativeFiles.Select(f => TestPath + "/" + f).ToArray();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.GetFiles(TestPath, true))
                  .Returns(fullPaths);
        }

        [Test]
        public void should_return_invalid_when_folder_does_not_exist()
        {
            GivenFolderExists(false);

            var result = Subject.ValidateReleaseStructure(TestPath, "FitGirl", "Some.Game-FitGirl");

            result.IsValid.Should().BeFalse();
            result.Message.Should().Contain("does not exist");
        }

        [Test]
        public void should_detect_valid_fitgirl_structure()
        {
            GivenFolderExists();
            GivenFiles("setup.exe", "fg-01.bin", "fg-02.bin", "fg-03.bin", "MD5");

            var result = Subject.ValidateReleaseStructure(TestPath, "FitGirl", "Some.Game-FitGirl");

            result.IsValid.Should().BeTrue();
            result.Confidence.Should().Be(ReleaseStructureConfidence.High);
            result.DetectedGroup.Should().Be("FitGirl");
        }

        [Test]
        public void should_detect_valid_codex_structure()
        {
            GivenFolderExists();
            GivenFiles("codex.nfo", "codex-1.iso", "Crack/game.exe");

            var result = Subject.ValidateReleaseStructure(TestPath, "CODEX", "Some.Game-CODEX");

            result.IsValid.Should().BeTrue();
            result.Confidence.Should().Be(ReleaseStructureConfidence.High);
            result.DetectedGroup.Should().Be("CODEX");
        }

        [Test]
        public void should_flag_suspicious_scr_file()
        {
            GivenFolderExists();
            GivenFiles("setup.exe", "fg-01.bin", "fg-02.bin", "bonus_content.scr");

            var result = Subject.ValidateReleaseStructure(TestPath, "FitGirl", "Some.Game-FitGirl");

            result.IsValid.Should().BeFalse();
            result.SuspiciousFiles.Should().Contain("bonus_content.scr");

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_flag_suspicious_vbs_file()
        {
            GivenFolderExists();
            GivenFiles("setup.exe", "fg-01.bin", "helper.vbs");

            var result = Subject.ValidateReleaseStructure(TestPath, "FitGirl", "Some.Game-FitGirl");

            result.IsValid.Should().BeFalse();
            result.SuspiciousFiles.Should().Contain("helper.vbs");

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_report_group_mismatch_when_codex_claimed_but_fitgirl_detected()
        {
            GivenFolderExists();
            GivenFiles("setup.exe", "fg-01.bin", "fg-02.bin");

            var result = Subject.ValidateReleaseStructure(TestPath, "CODEX", "Some.Game-CODEX");

            result.IsValid.Should().BeFalse();
            result.DetectedGroup.Should().Be("FitGirl");
            result.Message.Should().Contain("Claimed group");
            result.Message.Should().Contain("FitGirl");
            result.Confidence.Should().Be(ReleaseStructureConfidence.High);
        }

        [Test]
        public void should_return_unknown_confidence_for_unknown_group()
        {
            GivenFolderExists();
            GivenFiles("game.exe", "data.pak", "readme.txt");

            var result = Subject.ValidateReleaseStructure(TestPath, "SomeRandomGroup", "Some.Game-SomeRandomGroup");

            result.Confidence.Should().Be(ReleaseStructureConfidence.Low);
            result.Message.Should().Contain("Unknown release group");
        }

        [Test]
        public void should_mark_invalid_when_unknown_group_has_suspicious_files()
        {
            GivenFolderExists();
            GivenFiles("game.exe", "data.pak", "update.ps1");

            var result = Subject.ValidateReleaseStructure(TestPath, "SomeRandomGroup", "Some.Game-SomeRandomGroup");

            result.IsValid.Should().BeFalse();
            result.SuspiciousFiles.Should().Contain("update.ps1");

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_be_valid_for_unknown_group_without_suspicious_files()
        {
            GivenFolderExists();
            GivenFiles("game.exe", "data.pak", "readme.txt");

            var result = Subject.ValidateReleaseStructure(TestPath, "SomeRandomGroup", "Some.Game-SomeRandomGroup");

            result.IsValid.Should().BeTrue();
            result.SuspiciousFiles.Should().BeEmpty();
        }

        [Test]
        public void should_detect_forbidden_pattern_fitgirl_with_codex_nfo()
        {
            GivenFolderExists();
            GivenFiles("setup.exe", "fg-01.bin", "fg-02.bin", "codex.nfo");

            var result = Subject.ValidateReleaseStructure(TestPath, "FitGirl", "Some.Game-FitGirl");

            result.IsValid.Should().BeFalse();
            result.Message.Should().Contain("unexpected file");
            result.Message.Should().Contain("codex.nfo");
        }

        [Test]
        public void should_report_detected_group_when_no_claimed_group()
        {
            GivenFolderExists();
            GivenFiles("setup.exe", "fg-01.bin", "fg-02.bin", "MD5");

            var result = Subject.ValidateReleaseStructure(TestPath, null, "Some.Game");

            result.DetectedGroup.Should().Be("FitGirl");
            result.Confidence.Should().Be(ReleaseStructureConfidence.Medium);
            result.Message.Should().Contain("Detected as FitGirl");
        }

        [Test]
        public void should_fail_fitgirl_when_no_bin_files_present()
        {
            GivenFolderExists();
            GivenFiles("setup.exe", "readme.txt");

            var result = Subject.ValidateReleaseStructure(TestPath, "FitGirl", "Some.Game-FitGirl");

            result.IsValid.Should().BeFalse();
            result.Message.Should().Contain("structure matches");
        }

        [Test]
        public void should_flag_hta_file_as_suspicious()
        {
            GivenFolderExists();
            GivenFiles("codex.nfo", "codex-1.iso", "instructions.hta");

            var result = Subject.ValidateReleaseStructure(TestPath, "CODEX", "Some.Game-CODEX");

            result.IsValid.Should().BeFalse();
            result.SuspiciousFiles.Should().Contain("instructions.hta");

            ExceptionVerification.IgnoreWarns();
        }
    }
}
