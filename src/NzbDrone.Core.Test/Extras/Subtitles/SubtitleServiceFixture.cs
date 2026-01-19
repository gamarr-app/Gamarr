using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Subtitles;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras.Subtitles
{
    [TestFixture]
    public class SubtitleServiceFixture : CoreTest<SubtitleService>
    {
        private Game _game;
        private GameFile _gameFile;
        private LocalGame _localGame;

        private string _GameFolder;
        private string _releaseFolder;

        [SetUp]
        public void Setup()
        {
            _GameFolder = @"C:\Test\Games\Game Title".AsOsAgnostic();
            _releaseFolder = @"C:\Test\Unsorted Games\Game.Title.2022".AsOsAgnostic();

            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Path = _GameFolder)
                                     .Build();

            _gameFile = Builder<GameFile>.CreateNew()
                                               .With(f => f.Path = Path.Combine(_game.Path, "Game Title - 2022.mkv").AsOsAgnostic())
                                               .With(f => f.RelativePath = @"Game Title - 2022.mkv".AsOsAgnostic())
                                               .Build();

            _localGame = Builder<LocalGame>.CreateNew()
                                                 .With(l => l.Game = _game)
                                                 .With(l => l.Path = Path.Combine(_releaseFolder, "Game.Title.2022.mkv").AsOsAgnostic())
                                                 .With(l => l.FileGameInfo = new ParsedGameInfo
                                                 {
                                                     GameTitles = new List<string> { "Game Title" },
                                                     Year = 2022
                                                 })
                                                 .Build();

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetParentFolder(It.IsAny<string>()))
                  .Returns((string path) => Directory.GetParent(path).FullName);

            Mocker.GetMock<IDetectSample>().Setup(s => s.IsSample(It.IsAny<GameMetadata>(), It.IsAny<string>()))
                  .Returns(DetectSampleResult.NotSample);
        }

        [Test]
        [TestCase("Game.Title.2022.en.nfo")]
        public void should_not_import_non_subtitle_file(string filePath)
        {
            var files = new List<string> { Path.Combine(_releaseFolder, filePath).AsOsAgnostic() };

            var results = Subject.ImportFiles(_localGame, _gameFile, files, true).ToList();

            results.Count.Should().Be(0);
        }

        [Test]
        [TestCase("Game Title - 2022.srt", "Game Title - 2022.srt")]
        [TestCase("Game.Title.2022.en.srt", "Game Title - 2022.en.srt")]
        [TestCase("Game.Title.2022.english.srt", "Game Title - 2022.en.srt")]
        [TestCase("Game Title 2022_en_sdh_forced.srt", "Game Title - 2022.en.sdh.forced.srt")]
        [TestCase("Game_Title_2022 en.srt", "Game Title - 2022.en.srt")]
        [TestCase(@"Subs\Game.Title.2022\2_en.srt", "Game Title - 2022.en.srt")]
        [TestCase("sub.srt", "Game Title - 2022.srt")]
        public void should_import_matching_subtitle_file(string filePath, string expectedOutputPath)
        {
            var files = new List<string> { Path.Combine(_releaseFolder, filePath).AsOsAgnostic() };

            var results = Subject.ImportFiles(_localGame, _gameFile, files, true).ToList();

            results.Count.Should().Be(1);

            results[0].RelativePath.AsOsAgnostic().PathEquals(expectedOutputPath.AsOsAgnostic()).Should().Be(true);
        }

        [Test]
        public void should_import_multiple_subtitle_files_per_language()
        {
            var files = new List<string>
            {
                Path.Combine(_releaseFolder, "Game.Title.2022.en.srt").AsOsAgnostic(),
                Path.Combine(_releaseFolder, "Game.Title.2022.eng.srt").AsOsAgnostic(),
                Path.Combine(_releaseFolder, "Subs", "Game_Title_2022_en_forced.srt").AsOsAgnostic(),
                Path.Combine(_releaseFolder, "Subs", "Game.Title.2022", "2_fr.srt").AsOsAgnostic()
            };

            var expectedOutputs = new string[]
            {
                "Game Title - 2022.1.en.srt",
                "Game Title - 2022.2.en.srt",
                "Game Title - 2022.en.forced.srt",
                "Game Title - 2022.fr.srt",
            };

            var results = Subject.ImportFiles(_localGame, _gameFile, files, true).ToList();

            results.Count.Should().Be(expectedOutputs.Length);

            for (var i = 0; i < expectedOutputs.Length; i++)
            {
                results[i].RelativePath.AsOsAgnostic().PathEquals(expectedOutputs[i].AsOsAgnostic()).Should().Be(true);
            }
        }

        [Test]
        public void should_import_multiple_subtitle_files_per_language_with_tags()
        {
            var files = new List<string>
            {
                Path.Combine(_releaseFolder, "Game.Title.2022.en.forced.cc.srt").AsOsAgnostic(),
                Path.Combine(_releaseFolder, "Game.Title.2022.other.en.forced.cc.srt").AsOsAgnostic(),
                Path.Combine(_releaseFolder, "Game.Title.2022.en.forced.sdh.srt").AsOsAgnostic(),
                Path.Combine(_releaseFolder, "Game.Title.2022.en.forced.default.srt").AsOsAgnostic(),
            };

            var expectedOutputs = new[]
            {
                "Game Title - 2022.1.en.forced.cc.srt",
                "Game Title - 2022.2.en.forced.cc.srt",
                "Game Title - 2022.en.forced.sdh.srt",
                "Game Title - 2022.en.forced.default.srt"
            };

            var results = Subject.ImportFiles(_localGame, _gameFile, files, true).ToList();

            results.Count.Should().Be(expectedOutputs.Length);

            for (var i = 0; i < expectedOutputs.Length; i++)
            {
                results[i].RelativePath.AsOsAgnostic().PathEquals(expectedOutputs[i].AsOsAgnostic()).Should().Be(true);
            }
        }

        [Test]
        [TestCase(@"Subs\2_en.srt", "Game Title - 2022.en.srt")]
        public void should_import_unmatching_subtitle_file_if_only_episode(string filePath, string expectedOutputPath)
        {
            var subtitleFile = Path.Combine(_releaseFolder, filePath).AsOsAgnostic();

            var sampleFile = Path.Combine(_game.Path, "Game Title - 2022.sample.mkv").AsOsAgnostic();

            var videoFiles = new string[]
            {
                _localGame.Path,
                sampleFile
            };

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(It.IsAny<string>(), true))
                  .Returns(videoFiles);

            Mocker.GetMock<IDetectSample>().Setup(s => s.IsSample(It.IsAny<GameMetadata>(), sampleFile))
                  .Returns(DetectSampleResult.Sample);

            var results = Subject.ImportFiles(_localGame, _gameFile, new List<string> { subtitleFile }, true).ToList();

            results.Count.Should().Be(1);

            results[0].RelativePath.AsOsAgnostic().PathEquals(expectedOutputPath.AsOsAgnostic()).Should().Be(true);

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
