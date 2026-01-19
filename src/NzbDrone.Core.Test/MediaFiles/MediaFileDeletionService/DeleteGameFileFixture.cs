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
    public class DeleteGameFileFixture : CoreTest<Core.MediaFiles.MediaFileDeletionService>
    {
        private const string RootFolder = @"C:\Test\Games";
        private Game _game;
        private GameFile _gameFile;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Path = Path.Combine(RootFolder, "Game Title"))
                                     .Build();

            _gameFile = Builder<GameFile>.CreateNew()
                                               .With(f => f.RelativePath = "Some SubFolder")
                                               .With(f => f.Path = Path.Combine(_game.Path, "Some SubFolder"))
                                               .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetParentFolder(_game.Path))
                  .Returns(RootFolder);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetParentFolder(_gameFile.Path))
                  .Returns(_game.Path);
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

        [Test]
        public void should_throw_if_root_folder_does_not_exist()
        {
            Assert.Throws<NzbDroneClientException>(() => Subject.DeleteGameFile(_game, _gameFile));
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_should_throw_if_root_folder_is_empty()
        {
            GivenRootFolderExists();

            Assert.Throws<NzbDroneClientException>(() => Subject.DeleteGameFile(_game, _gameFile));
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_delete_from_db_if_game_folder_does_not_exist()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();

            Subject.DeleteGameFile(_game, _gameFile);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_gameFile, DeleteMediaFileReason.Manual), Times.Once());
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(_gameFile.Path, It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_delete_from_db_if_game_file_does_not_exist()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();
            GivenGameFolderExists();

            Subject.DeleteGameFile(_game, _gameFile);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_gameFile, DeleteMediaFileReason.Manual), Times.Once());
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(_gameFile.Path, It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_delete_from_disk_and_db_if_game_file_exists()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();
            GivenGameFolderExists();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(_gameFile.Path))
                  .Returns(true);

            Subject.DeleteGameFile(_game, _gameFile);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(_gameFile.Path, "Game Title"), Times.Once());
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_gameFile, DeleteMediaFileReason.Manual), Times.Once());
        }

        [Test]
        public void should_handle_error_deleting_game_file()
        {
            GivenRootFolderExists();
            GivenRootFolderHasFolders();
            GivenGameFolderExists();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(_gameFile.Path))
                  .Returns(true);

            Mocker.GetMock<IRecycleBinProvider>()
                  .Setup(s => s.DeleteFile(_gameFile.Path, "Game Title"))
                  .Throws(new IOException());

            Assert.Throws<NzbDroneClientException>(() => Subject.DeleteGameFile(_game, _gameFile));

            ExceptionVerification.ExpectedErrors(1);
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(_gameFile.Path, "Game Title"), Times.Once());
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_gameFile, DeleteMediaFileReason.Manual), Times.Never());
        }
    }
}
