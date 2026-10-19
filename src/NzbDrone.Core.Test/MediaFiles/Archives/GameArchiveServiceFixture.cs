using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles.Archives;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.Archives
{
    [TestFixture]
    public class GameArchiveServiceFixture : CoreTest<GameArchiveService>
    {
        // Committed asset, built with the 7-Zip CLI (not SharpCompress, which
        // would be testing the library against itself): a 1024-byte dummy .xci
        // alongside the junk a BlueRoms release ships, in a 288-byte archive.
        private const string EntryName = "Kirby Test Game [0100ABC].xci";
        private const long EntrySize = 1024;

        // A PC-shaped payload: an exe, data files and a subfolder, and not one
        // console ROM extension anywhere — the shape that could never satisfy the
        // exactly-one-ROM rule, so every re-grab of it failed identically.
        private const string PcArchive = "Files/TestPcGameArchive.7z";
        private const long PcArchiveTotalSize = 1856;
        private const int PcArchiveFileCount = 4;

        private string _archivePath;

        [SetUp]
        public void Setup()
        {
            _archivePath = GetTestPath("Files/TestGameArchive.7z");

            // Real disk behaviour, driven synchronously through the mock: the
            // archive reads and the extraction both have to touch real bytes.
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns<string>(Directory.Exists);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns<string>(File.Exists);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns<string, bool>((path, recursive) =>
                      Directory.GetFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.OpenWriteStream(It.IsAny<string>()))
                  .Returns<string>(File.Create);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.DeleteFile(It.IsAny<string>()))
                  .Callback<string>(File.Delete);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.MoveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                  .Callback<string, string, bool>((source, destination, overwrite) => File.Move(source, destination, overwrite));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.CreateFolder(It.IsAny<string>()))
                  .Callback<string>(path => Directory.CreateDirectory(path));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.DeleteFolder(It.IsAny<string>(), It.IsAny<bool>()))
                  .Callback<string, bool>((path, recursive) => Directory.Delete(path, recursive));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(It.IsAny<string>()))
                  .Returns<string>(Directory.GetDirectories);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.MoveFolder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                  .Callback<string, string, bool>((source, destination, overwrite) => Directory.Move(source, destination));
        }

        private string GivenReleaseFolder(bool withLooseGameFile = false)
        {
            // The exact shape of the release that caused this: one .7z beside a
            // filler .pad directory, a scene html file, metadata and artwork.
            var folder = Path.Combine(TempFolder, "Kirby Return to Dream Land Deluxe (BlueRoms)");

            Directory.CreateDirectory(Path.Combine(folder, ".pad"));
            Directory.CreateDirectory(Path.Combine(folder, "imgs"));

            File.Copy(_archivePath, Path.Combine(folder, "Kirby Return to Dream Land Deluxe.7z"));
            File.WriteAllText(Path.Combine(folder, ".pad", "0"), "filler");
            File.WriteAllText(Path.Combine(folder, "BLUEROMS.WS.html"), "<html></html>");
            File.WriteAllText(Path.Combine(folder, "metadata.json"), "{}");
            File.WriteAllText(Path.Combine(folder, "imgs", "cover.jpg"), "jpg");

            if (withLooseGameFile)
            {
                File.WriteAllText(Path.Combine(folder, "Kirby.nsp"), "nsp");
            }

            return folder;
        }

        [Test]
        public void should_report_the_single_game_file_inside_an_archive()
        {
            var inspection = Subject.InspectImportUnit(_archivePath);

            inspection.Should().NotBeNull();
            inspection.ArchivePath.Should().Be(_archivePath);
            inspection.EntryName.Should().Be(EntryName);
        }

        [Test]
        public void should_report_the_uncompressed_entry_size()
        {
            // The whole archive is 288 bytes on disk; budgeting free space or
            // comparing quality on that number is the bug being fixed.
            var inspection = Subject.InspectImportUnit(_archivePath);

            inspection.EntrySize.Should().Be(EntrySize);
            new FileInfo(_archivePath).Length.Should().BeLessThan(EntrySize);
        }

        [Test]
        public void should_find_the_archive_inside_a_folder_shaped_release()
        {
            // The regression that matters: a folder release never gets its files
            // enumerated by the import path, so the archive has to be found here.
            var folder = GivenReleaseFolder();

            var inspection = Subject.InspectImportUnit(folder);

            inspection.Should().NotBeNull();
            inspection.ArchivePath.Should().Be(Path.Combine(folder, "Kirby Return to Dream Land Deluxe.7z"));
            inspection.EntryName.Should().Be(EntryName);
            inspection.EntrySize.Should().Be(EntrySize);
        }

        [Test]
        public void should_not_touch_a_folder_that_already_holds_a_game_file()
        {
            var folder = GivenReleaseFolder(true);

            Subject.InspectImportUnit(folder).Should().BeNull();
        }

        [Test]
        public void should_not_inspect_a_folder_with_several_archives()
        {
            var folder = GivenReleaseFolder();
            File.Copy(_archivePath, Path.Combine(folder, "Kirby Return to Dream Land Deluxe.part2.7z"));

            Subject.InspectImportUnit(folder).Should().BeNull();
        }

        [Test]
        public void should_not_inspect_an_archive_with_no_game_file_inside()
        {
            Subject.InspectImportUnit(GetTestPath("Files/TestArchive.zip")).Should().BeNull();
        }

        [TestCase("Game.7z.001")]
        [TestCase("Game.zip.002")]
        [TestCase("Game.part1.rar")]
        [TestCase("Game.r00")]
        [TestCase("Game.z01")]
        public void should_refuse_split_archive_volumes(string fileName)
        {
            // v1 refuses these rather than half-handling a multi-volume extract.
            var path = Path.Combine(TempFolder, fileName);
            File.Copy(_archivePath, path);

            Subject.InspectImportUnit(path).Should().BeNull();
        }

        [TestCase("Game.xci")]
        [TestCase("Game.iso")]
        [TestCase("Game.nsp")]
        public void should_not_inspect_a_file_that_is_not_a_container(string fileName)
        {
            var path = Path.Combine(TempFolder, fileName);
            File.Copy(_archivePath, path);

            Subject.InspectImportUnit(path).Should().BeNull();
        }

        [Test]
        public void should_not_throw_on_a_corrupt_archive()
        {
            var path = Path.Combine(TempFolder, "Corrupt.7z");
            File.WriteAllText(path, "this is not a 7z file");

            Subject.InspectImportUnit(path).Should().BeNull();
        }

        [Test]
        public void should_extract_the_entry_and_leave_no_partial_file()
        {
            var inspection = Subject.InspectImportUnit(_archivePath);
            var destination = Path.Combine(TempFolder, "Kirby Test Game [Base].xci");

            Subject.ExtractEntry(inspection, destination);

            File.Exists(destination).Should().BeTrue();
            new FileInfo(destination).Length.Should().Be(EntrySize);
            File.Exists(destination + ".partial~").Should().BeFalse();
        }

        [Test]
        public void should_not_leave_a_partial_file_behind_when_extraction_fails()
        {
            // Otherwise the next disk scan adopts a truncated multi-gigabyte
            // write as the game file.
            var inspection = Subject.InspectImportUnit(_archivePath);
            inspection.EntryName = "NotInTheArchive.xci";

            var destination = Path.Combine(TempFolder, "Kirby Test Game [Base].xci");

            Assert.Throws<GameArchiveExtractionException>(() => Subject.ExtractEntry(inspection, destination));

            File.Exists(destination).Should().BeFalse();
            File.Exists(destination + ".partial~").Should().BeFalse();
        }

        [Test]
        public void should_replace_a_stale_partial_file()
        {
            var inspection = Subject.InspectImportUnit(_archivePath);
            var destination = Path.Combine(TempFolder, "Kirby Test Game [Base].xci");

            File.WriteAllText(destination + ".partial~", "leftovers from a failed run");

            Subject.ExtractEntry(inspection, destination);

            new FileInfo(destination).Length.Should().Be(EntrySize);
        }

        [Test]
        public void should_report_a_whole_archive_inspection_for_a_pc_payload()
        {
            var inspection = Subject.InspectImportUnit(GetTestPath(PcArchive), true);

            inspection.Should().NotBeNull();
            inspection.ExtractWholeArchive.Should().BeTrue();
            inspection.EntryName.Should().BeNull();
        }

        [Test]
        public void should_sum_the_uncompressed_entry_sizes_in_whole_archive_mode()
        {
            // FreeSpaceSpecification budgets on this; the 259-byte archive would
            // understate the tree that actually gets written.
            var inspection = Subject.InspectImportUnit(GetTestPath(PcArchive), true);

            inspection.EntrySize.Should().Be(PcArchiveTotalSize);
            new FileInfo(GetTestPath(PcArchive)).Length.Should().BeLessThan(PcArchiveTotalSize);
        }

        [Test]
        public void should_not_report_a_whole_archive_inspection_unless_the_caller_allows_it()
        {
            // A console release keeps the original reject-unless-one-ROM rule;
            // the platform gate that decides this lives in ImportDecisionMaker.
            Subject.InspectImportUnit(GetTestPath(PcArchive)).Should().BeNull();
        }

        [Test]
        public void should_still_prefer_the_single_rom_entry_when_whole_archive_is_allowed()
        {
            var inspection = Subject.InspectImportUnit(_archivePath, true);

            inspection.ExtractWholeArchive.Should().BeFalse();
            inspection.EntryName.Should().Be(EntryName);
            inspection.EntrySize.Should().Be(EntrySize);
        }

        [Test]
        public void should_refuse_an_archive_with_several_rom_entries_in_either_mode()
        {
            // Picking one would be a guess and merging them is not a folder
            // import, so whole-archive mode deliberately does not cover this.
            var path = GetTestPath("Files/TestMultiRomGameArchive.7z");

            Subject.InspectImportUnit(path).Should().BeNull();
            Subject.InspectImportUnit(path, true).Should().BeNull();
        }

        [TestCase("Lingo.7z.001")]
        [TestCase("Lingo.part1.rar")]
        [TestCase("Lingo.r00")]
        public void should_refuse_split_volumes_even_in_whole_archive_mode(string fileName)
        {
            var path = Path.Combine(TempFolder, fileName);
            File.Copy(GetTestPath(PcArchive), path);

            Subject.InspectImportUnit(path, true).Should().BeNull();
        }

        [Test]
        public void should_find_a_pc_archive_inside_a_folder_shaped_release()
        {
            var folder = Path.Combine(TempFolder, "Lingo 2 (2025) [FitGirl Repack]");
            Directory.CreateDirectory(folder);
            File.Copy(GetTestPath(PcArchive), Path.Combine(folder, "Lingo.2.rar"));

            var inspection = Subject.InspectImportUnit(folder, true);

            inspection.Should().NotBeNull();
            inspection.ExtractWholeArchive.Should().BeTrue();
        }

        [Test]
        public void should_extract_the_whole_archive_into_an_existing_game_folder()
        {
            var inspection = Subject.InspectImportUnit(GetTestPath(PcArchive), true);
            var destination = Path.Combine(TempFolder, "Lingo (2021)");

            // The mover has already created the game folder by this point.
            Directory.CreateDirectory(destination);

            Subject.ExtractArchive(inspection, destination);

            File.Exists(Path.Combine(destination, "Lingo.exe")).Should().BeTrue();
            File.Exists(Path.Combine(destination, "readme.txt")).Should().BeTrue();
            File.Exists(Path.Combine(destination, "data", "resource.dat")).Should().BeTrue();
            File.Exists(Path.Combine(destination, "data", "sub", "strings.bin")).Should().BeTrue();

            Directory.GetFiles(destination, "*", SearchOption.AllDirectories).Should().HaveCount(PcArchiveFileCount);
        }

        [Test]
        public void should_extract_the_whole_archive_when_the_game_folder_does_not_exist_yet()
        {
            var inspection = Subject.InspectImportUnit(GetTestPath(PcArchive), true);
            var destination = Path.Combine(TempFolder, "Lingo (2021)");

            Subject.ExtractArchive(inspection, destination);

            File.Exists(Path.Combine(destination, "Lingo.exe")).Should().BeTrue();
        }

        [Test]
        public void should_remove_the_partial_folder_after_a_whole_archive_extract()
        {
            // Otherwise the next DiskScanService.Scan adopts a half-written tree.
            var inspection = Subject.InspectImportUnit(GetTestPath(PcArchive), true);
            var destination = Path.Combine(TempFolder, "Lingo (2021)");

            Subject.ExtractArchive(inspection, destination);

            Directory.Exists(destination + ".partial~").Should().BeFalse();
        }

        [Test]
        public void should_not_leave_a_partial_folder_behind_when_a_whole_archive_extract_fails()
        {
            var inspection = Subject.InspectImportUnit(GetTestPath(PcArchive), true);
            inspection.ArchivePath = Path.Combine(TempFolder, "GoneMissing.7z");

            var destination = Path.Combine(TempFolder, "Lingo (2021)");

            Assert.Throws<GameArchiveExtractionException>(() => Subject.ExtractArchive(inspection, destination));

            Directory.Exists(destination + ".partial~").Should().BeFalse();
            Directory.Exists(destination).Should().BeFalse();
        }

        [Test]
        public void should_replace_a_stale_partial_folder()
        {
            var inspection = Subject.InspectImportUnit(GetTestPath(PcArchive), true);
            var destination = Path.Combine(TempFolder, "Lingo (2021)");

            Directory.CreateDirectory(destination + ".partial~");
            File.WriteAllText(Path.Combine(destination + ".partial~", "leftover.dat"), "from a failed run");

            Subject.ExtractArchive(inspection, destination);

            File.Exists(Path.Combine(destination, "leftover.dat")).Should().BeFalse();
            File.Exists(Path.Combine(destination, "Lingo.exe")).Should().BeTrue();
        }

        [Test]
        public void should_skip_archive_entries_that_escape_the_destination()
        {
            // An entry key is attacker-controlled: "../", a leading "/" and a
            // drive letter all write outside the game folder if taken verbatim.
            var inspection = Subject.InspectImportUnit(GetTestPath("Files/TestTraversalGameArchive.zip"), true);
            var destination = Path.Combine(TempFolder, "Lingo (2021)");

            Subject.ExtractArchive(inspection, destination);

            File.Exists(Path.Combine(destination, "safe", "ok.dat")).Should().BeTrue();
            File.Exists(Path.Combine(destination, "Lingo.exe")).Should().BeTrue();

            Directory.GetFiles(destination, "*", SearchOption.AllDirectories).Should().HaveCount(2);

            File.Exists(Path.Combine(TempFolder, "escaped.dat")).Should().BeFalse();
            File.Exists(Path.Combine(TempFolder, "escaped2.dat")).Should().BeFalse();
            File.Exists(Path.Combine(TempFolder, "absolute.dat")).Should().BeFalse();
        }
    }
}
