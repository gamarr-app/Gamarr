using System;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class UpgradeFolderMediaFileServiceFixture : CoreTest<UpgradeMediaFileService>
    {
        private GameFile _gameFile;
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _localGame = new LocalGame
            {
                Game = new Game
                {
                    Path = @"C:\Test\Games\Game".AsOsAgnostic()
                }
            };

            _gameFile = Builder<GameFile>.CreateNew().Build();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderExists(System.IO.Directory.GetParent(_localGame.Game.Path).FullName))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetParentFolder(It.IsAny<string>()))
                .Returns<string>(c => System.IO.Path.GetDirectoryName(c));
        }

        private void GivenFolderBasedGameFile()
        {
            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile = new GameFile
            {
                Id = 1,
                RelativePath = string.Empty // Folder-based
            };
        }

        private void GivenGameFolderHasContent()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderExists(_localGame.Game.Path))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(_localGame.Game.Path, true))
                .Returns(new[] { "game.exe", "data.bin" });
        }

        private void GivenGameFolderIsEmpty()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderExists(_localGame.Game.Path))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(_localGame.Game.Path, true))
                .Returns(Array.Empty<string>());
        }

        [Test]
        public void should_delete_folder_when_upgrading_folder_based_gamefile()
        {
            GivenFolderBasedGameFile();
            GivenGameFolderHasContent();

            Subject.UpgradeGameFile(_gameFile, _localGame);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(_localGame.Game.Path), Times.Once());
        }

        [Test]
        public void should_delete_folder_based_gamefile_from_database()
        {
            GivenFolderBasedGameFile();
            GivenGameFolderHasContent();

            Subject.UpgradeGameFile(_gameFile, _localGame);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<GameFile>(), DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_not_delete_folder_if_empty_when_upgrading()
        {
            GivenFolderBasedGameFile();
            GivenGameFolderIsEmpty();

            Subject.UpgradeGameFile(_gameFile, _localGame);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>()), Times.Never());
            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_still_delete_from_db_if_folder_is_empty()
        {
            GivenFolderBasedGameFile();
            GivenGameFolderIsEmpty();

            Subject.UpgradeGameFile(_gameFile, _localGame);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localGame.Game.GameFile, DeleteMediaFileReason.Upgrade), Times.Once());
            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_return_old_folder_gamefile_in_oldFiles()
        {
            GivenFolderBasedGameFile();
            GivenGameFolderHasContent();

            Subject.UpgradeGameFile(_gameFile, _localGame).OldFiles.Count.Should().Be(1);
        }

        [Test]
        public void should_not_call_delete_file_for_folder_based_upgrade()
        {
            GivenFolderBasedGameFile();
            GivenGameFolderHasContent();

            Subject.UpgradeGameFile(_gameFile, _localGame);

            // Should use DeleteFolder, not DeleteFile
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
    }
}
