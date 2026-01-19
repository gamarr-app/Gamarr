using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras.Others
{
    [TestFixture]
    public class OtherExtraServiceFixture : CoreTest<OtherExtraService>
    {
        private Game _game;
        private GameFile _gameFile;
        private LocalGame _localGame;

        private string _gameFolder;
        private string _releaseFolder;

        [SetUp]
        public void Setup()
        {
            _gameFolder = @"C:\Test\Games\Game Title".AsOsAgnostic();
            _releaseFolder = @"C:\Test\Unsorted Games\Game.Title.2022".AsOsAgnostic();

            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Path = _gameFolder)
                                     .Build();

            _gameFile = Builder<GameFile>.CreateNew()
                                               .With(f => f.Path = Path.Combine(_game.Path, "Game Title - 2022.mkv").AsOsAgnostic())
                                               .With(f => f.RelativePath = @"Game Title - 2022.mkv")
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
        }

        [Test]
        [TestCase("Game Title - 2022.nfo", "Game Title - 2022.nfo")]
        [TestCase("Game.Title.2022.nfo", "Game Title - 2022.nfo")]
        [TestCase("Game Title 2022.nfo", "Game Title - 2022.nfo")]
        [TestCase("Game_Title_2022.nfo", "Game Title - 2022.nfo")]
        [TestCase(@"Game.Title.2022\thumb.jpg", "Game Title - 2022.jpg")]
        public void should_import_matching_file(string filePath, string expectedOutputPath)
        {
            var files = new List<string> { Path.Combine(_releaseFolder, filePath).AsOsAgnostic() };

            var results = Subject.ImportFiles(_localGame, _gameFile, files, true).ToList();

            results.Count.Should().Be(1);

            results[0].RelativePath.AsOsAgnostic().PathEquals(expectedOutputPath.AsOsAgnostic()).Should().Be(true);
        }

        [Test]
        public void should_not_import_multiple_nfo_files()
        {
            var files = new List<string>
            {
                Path.Combine(_releaseFolder, "Game.Title.2022.nfo").AsOsAgnostic(),
                Path.Combine(_releaseFolder, "Game_Title_2022.nfo").AsOsAgnostic(),
            };

            var results = Subject.ImportFiles(_localGame, _gameFile, files, true).ToList();

            results.Count.Should().Be(1);
        }
    }
}
