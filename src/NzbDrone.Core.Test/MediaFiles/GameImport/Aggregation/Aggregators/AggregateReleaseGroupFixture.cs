using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Aggregation.Aggregators
{
    [TestFixture]
    public class AggregateReleaseGroupFixture : CoreTest<AggregateReleaseGroup>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew().Build();
        }

        [Test]
        public void should_prefer_downloadclient()
        {
            var fileGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL-Wizzy", false);
            var folderGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL-Drone", false);
            var downloadClientGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL-Viva", false);
            var localGame = new LocalGame
            {
                FileGameInfo = fileGameInfo,
                FolderGameInfo = folderGameInfo,
                DownloadClientGameInfo = downloadClientGameInfo,
                Path = @"C:\Test\Unsorted Games\Game.Title.2008\Game.Title.2008.WEB-DL.mkv".AsOsAgnostic(),
                Game = _game
            };

            Subject.Aggregate(localGame, null);

            localGame.ReleaseGroup.Should().Be("Viva");
        }

        [Test]
        public void should_prefer_folder()
        {
            var fileGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL-Wizzy", false);
            var folderGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL-Drone", false);
            var downloadClientGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL", false);
            var localGame = new LocalGame
            {
                FileGameInfo = fileGameInfo,
                FolderGameInfo = folderGameInfo,
                DownloadClientGameInfo = downloadClientGameInfo,
                Path = @"C:\Test\Unsorted Games\Game.Title.2008\Game.Title.2008.WEB-DL.mkv".AsOsAgnostic(),
                Game = _game
            };

            Subject.Aggregate(localGame, null);

            localGame.ReleaseGroup.Should().Be("Drone");
        }

        [Test]
        public void should_fallback_to_file()
        {
            var fileGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL-Wizzy", false);
            var folderGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL", false);
            var downloadClientGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL", false);
            var localGame = new LocalGame
            {
                FileGameInfo = fileGameInfo,
                FolderGameInfo = folderGameInfo,
                DownloadClientGameInfo = downloadClientGameInfo,
                Path = @"C:\Test\Unsorted Games\Game.Title.2008\Game.Title.2008.mkv".AsOsAgnostic(),
                Game = _game
            };

            Subject.Aggregate(localGame, null);

            localGame.ReleaseGroup.Should().Be("Wizzy");
        }

        [Test]
        public void should_not_use_imdb_prefix_as_release_group()
        {
            var fileGameInfo = Parser.Parser.ParseGameTitle("Elden Ring (2022) [imdb-tt0120735][Repack]-FraMeSToR", false);
            var folderGameInfo = Parser.Parser.ParseGameTitle("Elden Ring (2022) {imdb-tt0120735}", false);
            var downloadClientGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL", false);
            var localGame = new LocalGame
            {
                FileGameInfo = fileGameInfo,
                FolderGameInfo = folderGameInfo,
                DownloadClientGameInfo = downloadClientGameInfo,
                Path = @"C:\Test\Unsorted Games\Game.Title.2008\Game.Title.2008.mkv".AsOsAgnostic(),
                Game = _game
            };

            Subject.Aggregate(localGame, null);

            localGame.ReleaseGroup.Should().Be("FraMeSToR");
        }

        [Test]
        public void should_not_use_igdb_prefix_as_release_group()
        {
            var fileGameInfo = Parser.Parser.ParseGameTitle("Elden Ring (2022) [igdb-119133][Repack]-FraMeSToR", false);
            var folderGameInfo = Parser.Parser.ParseGameTitle("Elden Ring (2022) {igdb-119133}", false);
            var downloadClientGameInfo = Parser.Parser.ParseGameTitle("Game.Title.2008.WEB-DL", false);
            var localGame = new LocalGame
            {
                FileGameInfo = fileGameInfo,
                FolderGameInfo = folderGameInfo,
                DownloadClientGameInfo = downloadClientGameInfo,
                Path = @"C:\Test\Unsorted Games\Game.Title.2008\Game.Title.2008.mkv".AsOsAgnostic(),
                Game = _game
            };

            Subject.Aggregate(localGame, null);

            localGame.ReleaseGroup.Should().Be("FraMeSToR");
        }
    }
}
