using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class UpgradeMediaFileServiceFixture : CoreTest<UpgradeMediaFileService>
    {
        private GameFile _gameFile;
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _localGame = new LocalGame();
            _localGame.Game = new Game
            {
                Path = @"C:\Test\Games\Game".AsOsAgnostic()
            };

            _gameFile = Builder<GameFile>
                  .CreateNew()
                  .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FolderExists(Directory.GetParent(_localGame.Game.Path).FullName))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.GetParentFolder(It.IsAny<string>()))
                  .Returns<string>(c => Path.GetDirectoryName(c));
        }

        private void GivenSingleGameWithSingleGameFile()
        {
            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile =
                new GameFile
                {
                    Id = 1,
                    RelativePath = @"A.Game.2019.avi",
                };
        }

        [Test]
        public void should_delete_single_game_file_once()
        {
            GivenSingleGameWithSingleGameFile();

            Subject.UpgradeGameFile(_gameFile, _localGame);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_game_file_from_database()
        {
            GivenSingleGameWithSingleGameFile();

            Subject.UpgradeGameFile(_gameFile, _localGame);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<GameFile>(), DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_delete_existing_file_fromdb_if_file_doesnt_exist()
        {
            GivenSingleGameWithSingleGameFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeGameFile(_gameFile, _localGame);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localGame.Game.GameFile, DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_not_try_to_recyclebin_existing_file_if_file_doesnt_exist()
        {
            GivenSingleGameWithSingleGameFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeGameFile(_gameFile, _localGame);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_old_game_file_in_oldFiles()
        {
            GivenSingleGameWithSingleGameFile();

            Subject.UpgradeGameFile(_gameFile, _localGame).OldFiles.Count.Should().Be(1);
        }

        [Test]
        public void should_throw_if_there_are_existing_game_files_and_the_root_folder_is_missing()
        {
            GivenSingleGameWithSingleGameFile();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(c => c.FolderExists(Directory.GetParent(_localGame.Game.Path).FullName))
                  .Returns(false);

            Assert.Throws<RootFolderNotFoundException>(() => Subject.UpgradeGameFile(_gameFile, _localGame));

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localGame.Game.GameFile, DeleteMediaFileReason.Upgrade), Times.Never());
        }
    }
}
