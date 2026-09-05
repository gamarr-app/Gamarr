using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.DiskScanServiceTests
{
    [TestFixture]
    public class ScanFixture : CoreTest<DiskScanService>
    {
        private Game _game;
        private string _rootFolder;
        private string _otherGameFolder;

        [SetUp]
        public void Setup()
        {
            _rootFolder = @"C:\Test\Games".AsOsAgnostic();
            _otherGameFolder = @"C:\Test\Games\OtherGame".AsOsAgnostic();
            var gameFolder = @"C:\Test\Games\Game".AsOsAgnostic();

            _game = Builder<Game>.CreateNew()
                .With(s => s.Path = gameFolder)
                                     .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetParentFolder(It.IsAny<string>()))
                  .Returns((string path) => Directory.GetParent(path).FullName);

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), null))
                  .Returns(_rootFolder);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(It.IsAny<int>()))
                  .Returns(new List<GameFile>());

            Mocker.GetMock<NzbDrone.Core.Games.Components.IGameComponentRepository>()
                  .Setup(s => s.GetByGame(It.IsAny<int>()))
                  .Returns(new List<NzbDrone.Core.Games.Components.GameComponent>());
        }

        private void GivenRootFolder(params string[] subfolders)
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_rootFolder))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(_rootFolder))
                  .Returns(subfolders);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderEmpty(_rootFolder))
                  .Returns(subfolders.Empty());

            foreach (var folder in subfolders)
            {
                Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(folder))
                  .Returns(true);
            }
        }

        private void GivenGameFolder()
        {
            GivenRootFolder(_game.Path);
        }

        private void GivenPlatform(PlatformFamily platform)
        {
            _game.Platform = platform;
        }

        // Files plus their individual sizes, as a per-file platform sees them.
        private void GivenFilesWithSizes(params (string Name, long Size)[] files)
        {
            GivenFiles(files.Select(f => Path.Combine(_game.Path, f.Name).AsOsAgnostic()));

            foreach (var file in files)
            {
                var path = Path.Combine(_game.Path, file.Name).AsOsAgnostic();

                Mocker.GetMock<IDiskProvider>()
                      .Setup(s => s.GetFileSize(path))
                      .Returns(file.Size);

                Mocker.GetMock<IDiskProvider>()
                      .Setup(s => s.FileExists(path))
                      .Returns(true);
            }

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFolderSize(_game.Path))
                  .Returns(files.Sum(f => f.Size));
        }

        private void GivenExistingFiles(params GameFile[] files)
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(It.IsAny<int>()))
                  .Returns(files.ToList());
        }

        private void GivenFiles(IEnumerable<string> files)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                  .Returns(files.ToArray());
        }

        [Test]
        public void should_not_scan_if_game_root_folder_does_not_exist()
        {
            Subject.Scan(_game);

            ExceptionVerification.ExpectedWarns(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetFiles(_game.Path, true), Times.Never());

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.CreateFolder(_game.Path), Times.Never());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                .Verify(v => v.Clean(It.IsAny<Game>(), It.IsAny<List<string>>()), Times.Never());
        }

        [Test]
        public void should_not_scan_if_game_root_folder_is_empty()
        {
            GivenRootFolder();

            Subject.Scan(_game);

            ExceptionVerification.ExpectedWarns(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetFiles(_game.Path, true), Times.Never());

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.CreateFolder(_game.Path), Times.Never());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<Game>(), It.IsAny<List<string>>()), Times.Never());

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.IsAny<List<string>>(), _game, false), Times.Never());
        }

        [Test]
        public void should_create_if_game_folder_does_not_exist_but_create_folder_enabled()
        {
            GivenRootFolder(_otherGameFolder);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CreateEmptyGameFolders)
                  .Returns(true);

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.CreateFolder(_game.Path), Times.Once());
        }

        [Test]
        public void should_create_folder_gamefile_when_folder_has_content()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "EXTRAS", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Extras", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "EXTRAs", "file3.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "ExTrAs", "file4.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic()
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFolderSize(_game.Path))
                  .Returns(100000L);

            Subject.Scan(_game);

            // Should create a single folder-based GameFile
            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath == string.Empty)), Times.Once());
        }

        [Test]
        public void should_update_existing_folder_gamefile_when_size_changed()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "file1.iso").AsOsAgnostic(),
                       });

            var existingFile = new GameFile { Id = 1, GameId = _game.Id, RelativePath = string.Empty, Size = 50000L };
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { existingFile });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFolderSize(_game.Path))
                  .Returns(100000L);

            Subject.Scan(_game);

            // Should update the existing folder-based GameFile with new size
            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.Is<GameFile>(gf => gf.Id == 1 && gf.Size == 100000L)), Times.Once());
        }

        [Test]
        public void should_not_update_folder_gamefile_when_size_unchanged()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "file1.iso").AsOsAgnostic(),
                       });

            var existingFile = new GameFile { Id = 1, GameId = _game.Id, RelativePath = string.Empty, Size = 100000L };
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { existingFile });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFolderSize(_game.Path))
                  .Returns(100000L);

            Subject.Scan(_game);

            // Should not update if size is the same
            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Never());
        }

        [Test]
        public void should_not_create_if_game_folder_does_not_exist_and_create_folder_disabled()
        {
            GivenRootFolder(_otherGameFolder);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CreateEmptyGameFolders)
                  .Returns(false);

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.CreateFolder(_game.Path), Times.Never());
        }

        [Test]
        public void should_clean_but_not_create_gamefile_if_game_folder_does_not_exist()
        {
            GivenRootFolder(_otherGameFolder);

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.FolderExists(_game.Path), Times.Once());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<Game>(), It.IsAny<List<string>>()), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.IsAny<GameFile>()), Times.Never());
        }

        [Test]
        public void should_delete_existing_gamefiles_when_folder_is_empty()
        {
            GivenGameFolder();

            // Folder exists but has no files
            GivenFiles(new List<string>());

            var existingFile = new GameFile { Id = 1, GameId = _game.Id, RelativePath = string.Empty };
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { existingFile });

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Delete(existingFile, DeleteMediaFileReason.MissingFromDisk), Times.Once());
        }

        [Test]
        public void should_migrate_file_based_to_folder_based()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "file1.iso").AsOsAgnostic(),
                       });

            // Existing file-based GameFile (has non-empty RelativePath). The
            // legacy file is still on disk, so it goes through migration
            // (ManualOverride) rather than missing-from-disk cleanup.
            var existingFile = new GameFile { Id = 1, GameId = _game.Id, RelativePath = "old_file.exe" };
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { existingFile });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(Path.Combine(_game.Path, "old_file.exe")))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFolderSize(_game.Path))
                  .Returns(100000L);

            Subject.Scan(_game);

            // Should delete old file-based record
            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Delete(existingFile, DeleteMediaFileReason.ManualOverride), Times.Once());

            // Should create new folder-based record
            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath == string.Empty)), Times.Once());
        }

        [Test]
        public void should_publish_game_scanned_event()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "file1.iso").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFolderSize(_game.Path))
                  .Returns(100000L);

            Subject.Scan(_game);

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.IsAny<GameScannedEvent>()), Times.Once());
        }

        [Test]
        public void should_move_versioned_nested_update_folder_into_updates_layout()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "game.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "update_1.7.1", "update.iso").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(_game.Path))
                  .Returns(new[] { Path.Combine(_game.Path, "update_1.7.1").AsOsAgnostic() });

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.MoveFolder(
                      Path.Combine(_game.Path, "update_1.7.1").AsOsAgnostic(),
                      Path.Combine(_game.Path, "Updates", "v1.7.1").AsOsAgnostic(),
                      false), Times.Once());
        }

        [Test]
        public void should_not_move_unversioned_folders_or_game_content()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "game.iso").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(_game.Path))
                  .Returns(new[]
                  {
                      Path.Combine(_game.Path, "patch").AsOsAgnostic(),
                      Path.Combine(_game.Path, "DLC").AsOsAgnostic(),
                      Path.Combine(_game.Path, "data_1.5").AsOsAgnostic(),
                  });

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.MoveFolder(It.IsAny<string>(), It.IsAny<string>(), false), Times.Never());
        }

        [Test]
        public void should_adopt_untracked_component_folder_as_game_file()
        {
            GivenGameFolder();

            var updatesContainer = Path.Combine(_game.Path, "Updates").AsOsAgnostic();
            var updateDir = Path.Combine(_game.Path, "Updates", "v1.7.1").AsOsAgnostic();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "game.iso").AsOsAgnostic(),
                           Path.Combine(updateDir, "update.iso").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(updatesContainer))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(updatesContainer))
                  .Returns(new[] { updateDir });

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == Path.Combine("Updates", "v1.7.1") &&
                      gf.GameVersion.HasValue)), Times.Once());
        }

        [Test]
        public void should_not_adopt_component_folder_that_is_already_tracked()
        {
            GivenGameFolder();

            var updatesContainer = Path.Combine(_game.Path, "Updates").AsOsAgnostic();
            var updateDir = Path.Combine(_game.Path, "Updates", "v1.7.1").AsOsAgnostic();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "game.iso").AsOsAgnostic(),
                           Path.Combine(updateDir, "update.iso").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(updatesContainer))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(updateDir))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(updatesContainer))
                  .Returns(new[] { updateDir });

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(It.IsAny<int>()))
                  .Returns(new List<GameFile>
                  {
                      new GameFile { Id = 1, GameId = _game.Id, RelativePath = string.Empty },
                      new GameFile { Id = 2, GameId = _game.Id, RelativePath = Path.Combine("Updates", "v1.7.1") }
                  });

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath.Contains("Updates"))), Times.Never());
        }

        [Test]
        public void should_adopt_folder_when_other_component_files_are_tracked()
        {
            // Regression (Sentry 7620897427): comparing the candidate against a
            // tracked file with a *different* relative path used to hit
            // PathEquals, which throws on non-rooted paths and aborted the
            // whole rescan.
            GivenGameFolder();

            var updatesContainer = Path.Combine(_game.Path, "Updates").AsOsAgnostic();
            var updateDir = Path.Combine(_game.Path, "Updates", "v1.7.1").AsOsAgnostic();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "game.iso").AsOsAgnostic(),
                           Path.Combine(updateDir, "update.iso").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(updatesContainer))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(updatesContainer))
                  .Returns(new[] { updateDir });

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(It.IsAny<int>()))
                  .Returns(new List<GameFile>
                  {
                      new GameFile { Id = 1, GameId = _game.Id, RelativePath = string.Empty },
                      new GameFile { Id = 2, GameId = _game.Id, RelativePath = Path.Combine("DLC", "Playable Character Pack - Aaravi and Milo") }
                  });

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == Path.Combine("Updates", "v1.7.1"))), Times.Once());
        }

        private void GivenDlcSlot(string title)
        {
            Mocker.GetMock<NzbDrone.Core.Games.Components.IGameComponentRepository>()
                  .Setup(s => s.GetByGame(_game.Id))
                  .Returns(new List<NzbDrone.Core.Games.Components.GameComponent>
                  {
                      new NzbDrone.Core.Games.Components.GameComponent
                      {
                          Id = 12,
                          GameId = _game.Id,
                          ComponentType = NzbDrone.Core.Games.Components.GameComponentType.Dlc,
                          Key = "igdb:111",
                          Title = title
                      }
                  });
        }

        [Test]
        public void should_split_bundled_dlc_folder_matching_metadata_slot_with_packaged_payload()
        {
            GivenGameFolder();
            GivenDlcSlot("The Blood Price");

            var dlcDir = Path.Combine(_game.Path, "The.Blood.Price.DLC").AsOsAgnostic();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "game.iso").AsOsAgnostic(),
                           Path.Combine(dlcDir, "dlc.iso").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(_game.Path))
                  .Returns(new[] { dlcDir });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(dlcDir, false))
                  .Returns(new[] { Path.Combine(dlcDir, "dlc.iso").AsOsAgnostic() });

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.MoveFolder(
                      dlcDir,
                      Path.Combine(_game.Path, "DLC", "The.Blood.Price.DLC").AsOsAgnostic(),
                      false), Times.Once());
        }

        [Test]
        public void should_not_split_matching_folder_without_packaged_payload()
        {
            GivenGameFolder();
            GivenDlcSlot("The Blood Price");

            var dlcDir = Path.Combine(_game.Path, "The.Blood.Price.DLC").AsOsAgnostic();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "game.iso").AsOsAgnostic(),
                           Path.Combine(dlcDir, "content.pak").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(_game.Path))
                  .Returns(new[] { dlcDir });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(dlcDir, false))
                  .Returns(new[] { Path.Combine(dlcDir, "content.pak").AsOsAgnostic() });

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.MoveFolder(It.IsAny<string>(), It.IsAny<string>(), false), Times.Never());
        }

        [Test]
        public void should_not_split_folder_that_matches_no_dlc_slot()
        {
            GivenGameFolder();
            GivenDlcSlot("The Blood Price");

            var otherDir = Path.Combine(_game.Path, "soundtrack").AsOsAgnostic();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "game.iso").AsOsAgnostic(),
                           Path.Combine(otherDir, "music.iso").AsOsAgnostic(),
                       });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(_game.Path))
                  .Returns(new[] { otherDir });

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.MoveFolder(It.IsAny<string>(), It.IsAny<string>(), false), Times.Never());
        }

        // ---------------------------------------------------------------
        // Per-file granularity on console platforms (one packaged file IS
        // one game) vs folder granularity on PC (a repack is dozens of parts
        // of one install).
        // ---------------------------------------------------------------

        [Test]
        public void should_track_single_console_file_as_its_own_record()
        {
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(("Kirby and the Forgotten Land [v0].nsp", 6169789825L));

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == "Kirby and the Forgotten Land [v0].nsp" &&
                      gf.Size == 6169789825L)), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath == string.Empty)), Times.Never());
        }

        [Test]
        public void should_track_base_and_update_as_separate_records_with_individual_sizes()
        {
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(
                ("Pokemon Let's Go, Pikachu! [010003F003A34000][v0].nsp", 4465458938L),
                ("Pokemon Let's Go, Pikachu! [010003F003A34800][v131072].nsp", 35598272L));

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.Size == 4465458938L)), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.Size == 35598272L)), Times.Once());

            // The bug: 4465458938 + 35598272 collapsed into one folder row.
            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.Size == 4501057210L)), Times.Never());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath == string.Empty)), Times.Never());
        }

        [Test]
        public void should_treat_generic_nintendo_family_platform_as_per_file()
        {
            // Game 55 on the live instance is stored as plain `nintendo`, not
            // `nintendoSwitch` — an equality check against Switch would leave
            // it broken while appearing to work.
            GivenGameFolder();
            GivenPlatform(PlatformFamily.Nintendo);
            GivenFilesWithSizes(("Pokemon Let's Go, Pikachu! [v0].nsp", 4465458938L));

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == "Pokemon Let's Go, Pikachu! [v0].nsp" &&
                      gf.Size == 4465458938L)), Times.Once());
        }

        [TestCase(PlatformFamily.NintendoSwitch)]
        [TestCase(PlatformFamily.Nintendo)]
        [TestCase(PlatformFamily.NintendoGBA)]
        [TestCase(PlatformFamily.PlayStation)]
        [TestCase(PlatformFamily.SonyPSVita)]
        [TestCase(PlatformFamily.Xbox)]
        [TestCase(PlatformFamily.Sega)]
        [TestCase(PlatformFamily.Atari)]
        public void should_track_per_file_for_console_families(PlatformFamily platform)
        {
            GivenGameFolder();
            GivenPlatform(platform);
            GivenFilesWithSizes(("game.iso", 500L));

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath == "game.iso")), Times.Once());
        }

        [TestCase(PlatformFamily.PC)]
        [TestCase(PlatformFamily.Linux)]
        [TestCase(PlatformFamily.Mac)]
        [TestCase(PlatformFamily.Unknown)]
        public void should_keep_single_folder_record_for_non_console_platforms(PlatformFamily platform)
        {
            // The original motivation for folder records: a PC repack is
            // dozens of parts of one installation, not dozens of games.
            GivenGameFolder();
            GivenPlatform(platform);
            GivenFilesWithSizes(
                ("setup.exe", 1000L),
                ("data-1.bin", 2000L),
                ("data-2.bin", 3000L));

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == string.Empty &&
                      gf.Size == 6000L)), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath != string.Empty)), Times.Never());
        }

        [Test]
        public void should_not_destroy_existing_file_record_on_console_rescan()
        {
            // Regression: game 60 (Kirby) came in file-granular via download
            // import and every rescan used to delete that record and replace
            // it with a folder record summing base + update.
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(
                ("Kirby and the Forgotten Land [v0].nsp", 6169789825L),
                ("Kirby and the Forgotten Land [v65536][1.1.0][UPD].nsp", 50953289L));

            var existing = new GameFile
            {
                Id = 145,
                GameId = _game.Id,
                RelativePath = "Kirby and the Forgotten Land [v0].nsp",
                Size = 6169789825L
            };

            _game.GameFileId = existing.Id;
            GivenExistingFiles(existing);

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Never());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath == string.Empty)), Times.Never());

            // Untouched size means no needless write; the update gets its own row.
            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Never());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == "Kirby and the Forgotten Land [v65536][1.1.0][UPD].nsp" &&
                      gf.Size == 50953289L)), Times.Once());
        }

        [Test]
        public void should_track_byte_identical_duplicate_files_as_separate_records()
        {
            // Two files, same size, different names: reconciliation keys on
            // relative path, so neither is silently dropped.
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(
                ("Kirby and the Forgotten Land [01004D300C5AE000][v0][Base].nsp", 6169789825L),
                ("Kirby and the Forgotten Land [v0].nsp", 6169789825L));

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == "Kirby and the Forgotten Land [01004D300C5AE000][v0][Base].nsp")), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == "Kirby and the Forgotten Land [v0].nsp")), Times.Once());
        }

        [Test]
        public void should_explode_folder_record_into_per_file_records_on_console_platform()
        {
            // Repair path for game 55 (Pokémon): one folder record whose size
            // is base + update becomes two records with their real sizes. The
            // folder row is repurposed for the base rather than deleted, so
            // Game.GameFileId keeps pointing at the base.
            GivenGameFolder();
            GivenPlatform(PlatformFamily.Nintendo);
            GivenFilesWithSizes(
                ("Pokemon Let's Go, Pikachu! [010003F003A34000][v0].nsp", 4465458938L),
                ("Pokemon Let's Go, Pikachu! [010003F003A34800][v131072].nsp", 35598272L));

            var folderRecord = new GameFile
            {
                Id = 146,
                GameId = _game.Id,
                RelativePath = string.Empty,
                Size = 4501057210L,
                SceneName = "Pokemon.Lets.Go.Pikachu.NSW-GRP"
            };

            _game.GameFileId = folderRecord.Id;
            GivenExistingFiles(folderRecord);

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.Is<GameFile>(gf =>
                      gf.Id == 146 &&
                      gf.RelativePath == "Pokemon Let's Go, Pikachu! [010003F003A34000][v0].nsp" &&
                      gf.Size == 4465458938L &&
                      gf.SceneName == "Pokemon.Lets.Go.Pikachu.NSW-GRP")), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf =>
                      gf.RelativePath == "Pokemon Let's Go, Pikachu! [010003F003A34800][v131072].nsp" &&
                      gf.Size == 35598272L)), Times.Once());

            // The folder row is recycled, never deleted — deleting it would
            // null out Game.GameFileId.
            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        [Test]
        public void should_delete_per_file_record_whose_file_vanished()
        {
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(("kept.nsp", 100L));

            var gone = new GameFile { Id = 7, GameId = _game.Id, RelativePath = "gone.nsp", Size = 200L };
            var kept = new GameFile { Id = 8, GameId = _game.Id, RelativePath = "kept.nsp", Size = 100L };

            GivenExistingFiles(gone, kept);

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Delete(gone, DeleteMediaFileReason.MissingFromDisk), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Delete(kept, It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        [Test]
        public void should_update_size_of_existing_per_file_record_when_it_changed()
        {
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(("game.nsp", 900L));

            var existing = new GameFile { Id = 3, GameId = _game.Id, RelativePath = "game.nsp", Size = 100L };
            GivenExistingFiles(existing);

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.Is<GameFile>(gf => gf.Id == 3 && gf.Size == 900L)), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.IsAny<GameFile>()), Times.Never());
        }

        [Test]
        public void should_leave_tracked_component_subfolder_alone_on_console_platform()
        {
            // Updates/<version> is its own unit; files inside it must not also
            // get per-file records.
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);

            var updateDir = Path.Combine(_game.Path, "Updates", "v1.1.0").AsOsAgnostic();

            GivenFilesWithSizes(
                ("game.nsp", 900L),
                (Path.Combine("Updates", "v1.1.0", "update.nsp"), 50L));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(updateDir))
                  .Returns(true);

            GivenExistingFiles(new GameFile
            {
                Id = 4,
                GameId = _game.Id,
                RelativePath = Path.Combine("Updates", "v1.1.0"),
                Size = 50L
            });

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath == "game.nsp")), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath.Contains("Updates"))), Times.Never());

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        [Test]
        public void should_repoint_dangling_primary_file_at_base_record()
        {
            // With N records per game, Game.GameFileId can name a row that has
            // since been deleted (its file vanished). Leaving it dangling makes
            // the game look file-less while its files sit on disk.
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(
                ("base.nsp", 900L),
                ("update.nsp", 50L));

            _game.GameFileId = 99;

            GivenExistingFiles(
                new GameFile { Id = 8, GameId = _game.Id, RelativePath = "base.nsp", Size = 900L },
                new GameFile { Id = 9, GameId = _game.Id, RelativePath = "update.nsp", Size = 50L });

            Subject.Scan(_game);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(It.Is<Game>(g => g.GameFileId == 8)), Times.Once());
        }

        [Test]
        public void should_not_touch_primary_file_that_still_names_a_tracked_record()
        {
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(
                ("base.nsp", 900L),
                ("update.nsp", 50L));

            _game.GameFileId = 9;

            GivenExistingFiles(
                new GameFile { Id = 8, GameId = _game.Id, RelativePath = "base.nsp", Size = 900L },
                new GameFile { Id = 9, GameId = _game.Id, RelativePath = "update.nsp", Size = 50L });

            Subject.Scan(_game);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(It.IsAny<Game>()), Times.Never());
        }

        [Test]
        public void should_fall_back_to_folder_record_on_console_platform_without_recognisable_game_files()
        {
            GivenGameFolder();
            GivenPlatform(PlatformFamily.NintendoSwitch);
            GivenFilesWithSizes(("readme.txt", 10L));

            Subject.Scan(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Add(It.Is<GameFile>(gf => gf.RelativePath == string.Empty && gf.Size == 10L)), Times.Once());
        }
    }
}
