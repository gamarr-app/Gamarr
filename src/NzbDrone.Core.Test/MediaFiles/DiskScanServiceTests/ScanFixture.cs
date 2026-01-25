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

            // Existing file-based GameFile (has non-empty RelativePath)
            var existingFile = new GameFile { Id = 1, GameId = _game.Id, RelativePath = "old_file.exe" };
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { existingFile });

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
    }
}
