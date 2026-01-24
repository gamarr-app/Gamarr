using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.VirusScanning;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.VirusScanning
{
    [TestFixture]
    public class ClamAvScannerServiceFixture : CoreTest<ClamAvScannerService>
    {
        private const string CLAMSCAN_PATH = "/usr/bin/clamscan";
        private const string SCAN_TARGET = "/data/downloads/game.zip";

        private void GivenScannerAvailable()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.VirusScanEnabled)
                  .Returns(true);

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.VirusScannerPath)
                  .Returns(CLAMSCAN_PATH);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(CLAMSCAN_PATH))
                  .Returns(true);
        }

        private void GivenExtraArguments(string args)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.VirusScannerArguments)
                  .Returns(args);
        }

        private ProcessOutput GivenProcessOutput(int exitCode, params string[] lines)
        {
            var output = new ProcessOutput { ExitCode = exitCode };

            foreach (var line in lines)
            {
                output.Lines.Add(new ProcessOutputLine(ProcessOutputLevel.Standard, line));
            }

            return output;
        }

        [Test]
        public void should_return_clean_when_scanner_not_available()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.VirusScanEnabled)
                  .Returns(false);

            var result = Subject.ScanPath(SCAN_TARGET);

            result.IsClean.Should().BeTrue();
            result.ScanCompleted.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public void should_report_not_available_when_virus_scan_disabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.VirusScanEnabled)
                  .Returns(false);

            Subject.IsAvailable.Should().BeFalse();
        }

        [Test]
        public void should_report_available_when_enabled_and_path_exists()
        {
            GivenScannerAvailable();

            Subject.IsAvailable.Should().BeTrue();
        }

        [Test]
        public void should_return_clean_for_exit_code_zero()
        {
            GivenScannerAvailable();
            GivenExtraArguments(string.Empty);

            var processOutput = GivenProcessOutput(0,
                "Scanned files: 5",
                "Infected files: 0");

            Mocker.GetMock<IProcessProvider>()
                  .Setup(s => s.StartAndCapture(CLAMSCAN_PATH, It.IsAny<string>(), null))
                  .Returns(processOutput);

            var result = Subject.ScanPath(SCAN_TARGET);

            result.IsClean.Should().BeTrue();
            result.ScanCompleted.Should().BeTrue();
            result.ScannedFileCount.Should().Be(5);
            result.InfectedFiles.Should().BeEmpty();
        }

        [Test]
        public void should_detect_single_infected_file()
        {
            GivenScannerAvailable();
            GivenExtraArguments(string.Empty);

            var processOutput = GivenProcessOutput(1,
                "/data/downloads/game.zip: Win.Trojan.Agent-123456 FOUND",
                "Scanned files: 1");

            Mocker.GetMock<IProcessProvider>()
                  .Setup(s => s.StartAndCapture(CLAMSCAN_PATH, It.IsAny<string>(), null))
                  .Returns(processOutput);

            var result = Subject.ScanPath(SCAN_TARGET);

            result.IsClean.Should().BeFalse();
            result.ScanCompleted.Should().BeTrue();
            result.InfectedFiles.Should().HaveCount(1);
            result.InfectedFiles[0].FilePath.Should().Be("/data/downloads/game.zip");
            result.InfectedFiles[0].ThreatName.Should().Be("Win.Trojan.Agent-123456");
        }

        [Test]
        public void should_detect_multiple_infected_files()
        {
            GivenScannerAvailable();
            GivenExtraArguments(string.Empty);

            var processOutput = GivenProcessOutput(1,
                "/data/downloads/file1.exe: Win.Malware.GenA FOUND",
                "/data/downloads/file2.dll: Win.Trojan.Downloader FOUND",
                "Scanned files: 10");

            Mocker.GetMock<IProcessProvider>()
                  .Setup(s => s.StartAndCapture(CLAMSCAN_PATH, It.IsAny<string>(), null))
                  .Returns(processOutput);

            var result = Subject.ScanPath(SCAN_TARGET);

            result.IsClean.Should().BeFalse();
            result.ScanCompleted.Should().BeTrue();
            result.InfectedFiles.Should().HaveCount(2);
            result.InfectedFiles[0].ThreatName.Should().Be("Win.Malware.GenA");
            result.InfectedFiles[1].ThreatName.Should().Be("Win.Trojan.Downloader");
            result.ScannedFileCount.Should().Be(10);
        }

        [Test]
        public void should_mark_scan_incomplete_on_exit_code_two()
        {
            GivenScannerAvailable();
            GivenExtraArguments(string.Empty);

            var processOutput = GivenProcessOutput(2,
                "ERROR: Can't open file or directory");

            Mocker.GetMock<IProcessProvider>()
                  .Setup(s => s.StartAndCapture(CLAMSCAN_PATH, It.IsAny<string>(), null))
                  .Returns(processOutput);

            var result = Subject.ScanPath(SCAN_TARGET);

            result.ScanCompleted.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public void should_handle_process_exception_gracefully()
        {
            GivenScannerAvailable();
            GivenExtraArguments(string.Empty);

            Mocker.GetMock<IProcessProvider>()
                  .Setup(s => s.StartAndCapture(CLAMSCAN_PATH, It.IsAny<string>(), null))
                  .Throws(new InvalidOperationException("Process failed to start"));

            var result = Subject.ScanPath(SCAN_TARGET);

            result.ScanCompleted.Should().BeFalse();
            result.ErrorMessage.Should().Be("Process failed to start");
        }

        [Test]
        public void should_auto_detect_clamscan_from_common_paths()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.VirusScanEnabled)
                  .Returns(true);

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.VirusScannerPath)
                  .Returns(string.Empty);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists("/usr/bin/clamscan"))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists("/usr/local/bin/clamscan"))
                  .Returns(true);

            Subject.IsAvailable.Should().BeTrue();
            Subject.DetectedScannerPath.Should().Be("/usr/local/bin/clamscan");
        }

        [Test]
        public void should_include_extra_arguments_in_command()
        {
            GivenScannerAvailable();
            GivenExtraArguments("--max-filesize=100M --max-scansize=200M");

            var processOutput = GivenProcessOutput(0, "Scanned files: 1");

            Mocker.GetMock<IProcessProvider>()
                  .Setup(s => s.StartAndCapture(CLAMSCAN_PATH, It.IsAny<string>(), null))
                  .Returns(processOutput);

            Subject.ScanPath(SCAN_TARGET);

            Mocker.GetMock<IProcessProvider>()
                  .Verify(v => v.StartAndCapture(
                      CLAMSCAN_PATH,
                      It.Is<string>(args =>
                          args.Contains("--recursive") &&
                          args.Contains("--infected") &&
                          args.Contains("--no-summary=false") &&
                          args.Contains("--max-filesize=100M --max-scansize=200M") &&
                          args.Contains($"\"{SCAN_TARGET}\"")),
                      null),
                  Times.Once());
        }

        [Test]
        public void should_parse_scan_summary_file_count()
        {
            GivenScannerAvailable();
            GivenExtraArguments(string.Empty);

            var processOutput = GivenProcessOutput(0,
                "----------- SCAN SUMMARY -----------",
                "Known viruses: 8000000",
                "Scanned directories: 3",
                "Scanned files: 42",
                "Infected files: 0",
                "Data scanned: 128.50 MB");

            Mocker.GetMock<IProcessProvider>()
                  .Setup(s => s.StartAndCapture(CLAMSCAN_PATH, It.IsAny<string>(), null))
                  .Returns(processOutput);

            var result = Subject.ScanPath(SCAN_TARGET);

            result.ScannedFileCount.Should().Be(42);
            result.IsClean.Should().BeTrue();
            result.ScanCompleted.Should().BeTrue();
        }
    }
}
