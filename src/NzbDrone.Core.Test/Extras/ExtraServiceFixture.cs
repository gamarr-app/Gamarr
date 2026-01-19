using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras
{
    [TestFixture]
    public class ExtraServiceFixture : CoreTest<ExtraService>
    {
        private Game _game;
        private GameFile _gameFile;
        private LocalGame _localGame;

        private string _gameFolder;
        private string _releaseFolder;

        private Mock<IManageExtraFiles> _subtitleService;
        private Mock<IManageExtraFiles> _otherExtraService;

        [SetUp]
        public void Setup()
        {
            _gameFolder = @"C:\Test\Games\Game Title".AsOsAgnostic();
            _releaseFolder = @"C:\Test\Unsorted TV\Game.Title.2022".AsOsAgnostic();

            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Path = _gameFolder)
                                     .Build();

            _gameFile = Builder<GameFile>.CreateNew()
                                               .With(f => f.Path = Path.Combine(_game.Path, "Game Title - 2022.mkv").AsOsAgnostic())
                                               .With(f => f.RelativePath = @"Game Title - 2022.mkv".AsOsAgnostic())
                                               .Build();

            _localGame = Builder<LocalGame>.CreateNew()
                                                 .With(l => l.Game = _game)
                                                 .With(l => l.Path = Path.Combine(_releaseFolder, "Game.Title.2022.mkv").AsOsAgnostic())
                                                 .Build();

            _subtitleService = new Mock<IManageExtraFiles>();
            _subtitleService.SetupGet(s => s.Order).Returns(0);
            _subtitleService.Setup(s => s.CanImportFile(It.IsAny<LocalGame>(), It.IsAny<GameFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(false);
            _subtitleService.Setup(s => s.CanImportFile(It.IsAny<LocalGame>(), It.IsAny<GameFile>(), It.IsAny<string>(), ".srt", It.IsAny<bool>()))
                .Returns(true);

            _otherExtraService = new Mock<IManageExtraFiles>();
            _otherExtraService.SetupGet(s => s.Order).Returns(1);
            _otherExtraService.Setup(s => s.CanImportFile(It.IsAny<LocalGame>(), It.IsAny<GameFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(true);

            Mocker.SetConstant<IEnumerable<IManageExtraFiles>>(new[]
            {
                _subtitleService.Object,
                _otherExtraService.Object
            });

            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetParentFolder(It.IsAny<string>()))
                  .Returns((string path) => Directory.GetParent(path).FullName);

            WithExistingFolder(_game.Path);
            WithExistingFile(_gameFile.Path);
            WithExistingFile(_localGame.Path);

            Mocker.GetMock<IConfigService>().Setup(v => v.ImportExtraFiles).Returns(true);
            Mocker.GetMock<IConfigService>().Setup(v => v.ExtraFileExtensions).Returns("nfo,srt");
        }

        private void WithExistingFolder(string path, bool exists = true)
        {
            var dir = Path.GetDirectoryName(path);

            if (exists && dir.IsNotNullOrWhiteSpace())
            {
                WithExistingFolder(dir);
            }

            Mocker.GetMock<IDiskProvider>().Setup(v => v.FolderExists(path)).Returns(exists);
        }

        private void WithExistingFile(string path, bool exists = true, int size = 1000)
        {
            var dir = Path.GetDirectoryName(path);

            if (exists && dir.IsNotNullOrWhiteSpace())
            {
                WithExistingFolder(dir);
            }

            Mocker.GetMock<IDiskProvider>().Setup(v => v.FileExists(path)).Returns(exists);
            Mocker.GetMock<IDiskProvider>().Setup(v => v.GetFileSize(path)).Returns(size);
        }

        private void WithExistingFiles(List<string> files)
        {
            foreach (var file in files)
            {
                WithExistingFile(file);
            }

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(_releaseFolder, It.IsAny<bool>()))
                  .Returns(files.ToArray());
        }

        [Test]
        public void should_not_pass_file_if_import_disabled()
        {
            Mocker.GetMock<IConfigService>().Setup(v => v.ImportExtraFiles).Returns(false);

            var nfofile = Path.Combine(_releaseFolder, "Game.Title.2022.nfo").AsOsAgnostic();

            var files = new List<string>
            {
                _localGame.Path,
                nfofile
            };

            WithExistingFiles(files);

            Subject.ImportGame(_localGame, _gameFile, true);

            _subtitleService.Verify(v => v.CanImportFile(_localGame, _gameFile, It.IsAny<string>(), It.IsAny<string>(), true), Times.Never());
            _otherExtraService.Verify(v => v.CanImportFile(_localGame, _gameFile, It.IsAny<string>(), It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        [TestCase("Game Title - 2022.sub")]
        [TestCase("Game Title - 2022.ass")]
        public void should_not_pass_unwanted_file(string filePath)
        {
            Mocker.GetMock<IConfigService>().Setup(v => v.ImportExtraFiles).Returns(false);

            var nfofile = Path.Combine(_releaseFolder, filePath).AsOsAgnostic();

            var files = new List<string>
            {
                _localGame.Path,
                nfofile
            };

            WithExistingFiles(files);

            Subject.ImportGame(_localGame, _gameFile, true);

            _subtitleService.Verify(v => v.CanImportFile(_localGame, _gameFile, It.IsAny<string>(), It.IsAny<string>(), true), Times.Never());
            _otherExtraService.Verify(v => v.CanImportFile(_localGame, _gameFile, It.IsAny<string>(), It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_pass_subtitle_file_to_subtitle_service()
        {
            var subtitleFile = Path.Combine(_releaseFolder, "Game.Title.2022.en.srt").AsOsAgnostic();

            var files = new List<string>
            {
                _localGame.Path,
                subtitleFile
            };

            WithExistingFiles(files);

            Subject.ImportGame(_localGame, _gameFile, true);

            _subtitleService.Verify(v => v.ImportFiles(_localGame, _gameFile, new List<string> { subtitleFile }, true), Times.Once());
            _otherExtraService.Verify(v => v.ImportFiles(_localGame, _gameFile, new List<string> { subtitleFile }, true), Times.Never());
        }

        [Test]
        public void should_pass_nfo_file_to_other_service()
        {
            var nfofile = Path.Combine(_releaseFolder, "Game.Title.2022.nfo").AsOsAgnostic();

            var files = new List<string>
            {
                _localGame.Path,
                nfofile
            };

            WithExistingFiles(files);

            Subject.ImportGame(_localGame, _gameFile, true);

            _subtitleService.Verify(v => v.ImportFiles(_localGame, _gameFile, new List<string> { nfofile }, true), Times.Never());
            _otherExtraService.Verify(v => v.ImportFiles(_localGame, _gameFile, new List<string> { nfofile }, true), Times.Once());
        }

        [Test]
        public void should_search_subtitles_when_importing_from_job_folder()
        {
            _localGame.FolderGameInfo = new ParsedGameInfo();

            var subtitleFile = Path.Combine(_releaseFolder, "Game.Title.2022.en.srt").AsOsAgnostic();

            var files = new List<string>
            {
                _localGame.Path,
                subtitleFile
            };

            WithExistingFiles(files);

            Subject.ImportGame(_localGame, _gameFile, true);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(_releaseFolder, true), Times.Once);
            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(_releaseFolder, false), Times.Never);
        }

        [Test]
        public void should_not_search_subtitles_when_not_importing_from_job_folder()
        {
            _localGame.FolderGameInfo = null;

            var subtitleFile = Path.Combine(_releaseFolder, "Game.Title.2022.en.srt").AsOsAgnostic();

            var files = new List<string>
            {
                _localGame.Path,
                subtitleFile
            };

            WithExistingFiles(files);

            Subject.ImportGame(_localGame, _gameFile, true);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(_releaseFolder, true), Times.Never);
            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(_releaseFolder, false), Times.Once);
        }
    }
}
