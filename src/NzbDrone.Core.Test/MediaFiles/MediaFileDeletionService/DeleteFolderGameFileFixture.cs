using System;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaFileDeletionService
{
    [TestFixture]
    public class DeleteFolderGameFileFixture : CoreTest<Core.MediaFiles.MediaFileDeletionService>
    {
        private const string RootFolder = @"C:\Test\Games";
        private Game _game;
        private GameFile _folderGameFile;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                .With(s => s.Path = Path.Combine(RootFolder, "Game Title"))
                .Build();

            // Folder-based GameFile has empty RelativePath
            _folderGameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = string.Empty)
                .With(f => f.Path = _game.Path)
                .Build();

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetParentFolder(_game.Path))
                .Returns(RootFolder);
        }

        private void GivenRootFolderExists()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(RootFolder))
                .Returns(true);
        }

        private void GivenRootFolderHasFolders()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetDirectories(RootFolder))
                .Returns(new[] { _game.Path });
        }

        private void GivenGameFolderExists()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_game.Path))
                .Returns(true);
        }

        private void GivenGameFolderHasContent()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetFiles(_game.Path, true))
                .Returns(new[] { Path.Combine(_game.Path, "game.exe"), Path.Combine(_game.Path, "data.bin") });
        }

        [Test]
        public void should_delete_folder_from_disk_when_folder_based_gamefile()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();
            GivenGameFolderExists();
            GivenGameFolderHasContent();

            Subject.DeleteGameFile(_game, _folderGameFile);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(_game.Path), Times.Once());
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_folderGameFile, DeleteMediaFileReason.Manual), Times.Once());
        }

        [Test]
        public void should_not_delete_folder_if_empty()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();
            GivenGameFolderExists();

            // Folder exists but is empty
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetFiles(_game.Path, true))
                .Returns(Array.Empty<string>());

            Subject.DeleteGameFile(_game, _folderGameFile);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>()), Times.Never());
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_folderGameFile, DeleteMediaFileReason.Manual), Times.Once());
        }

        [Test]
        public void should_delete_from_db_only_if_game_folder_does_not_exist()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();

            // Game folder doesn't exist
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_game.Path))
                .Returns(false);

            Subject.DeleteGameFile(_game, _folderGameFile);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>()), Times.Never());
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_folderGameFile, DeleteMediaFileReason.Manual), Times.Once());
        }

        [Test]
        public void should_handle_error_deleting_folder()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();
            GivenGameFolderExists();
            GivenGameFolderHasContent();

            Mocker.GetMock<IRecycleBinProvider>()
                .Setup(s => s.DeleteFolder(_game.Path))
                .Throws(new IOException());

            Assert.Throws<NzbDroneClientException>(() => Subject.DeleteGameFile(_game, _folderGameFile));

            ExceptionVerification.ExpectedErrors(1);
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(_game.Path), Times.Once());
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_folderGameFile, DeleteMediaFileReason.Manual), Times.Never());
        }

        [Test]
        public void should_not_call_delete_file_for_folder_based_gamefile()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();
            GivenGameFolderExists();
            GivenGameFolderHasContent();

            Subject.DeleteGameFile(_game, _folderGameFile);

            // Should use DeleteFolder, not DeleteFile
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
    }
}
