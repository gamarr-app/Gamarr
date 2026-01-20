using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GamesTests
{
    [TestFixture]
    public class AddGameServiceFixture : CoreTest<AddGameService>
    {
        private Game _newGame;
        private GameMetadata _gameMetadata;

        [SetUp]
        public void Setup()
        {
            _gameMetadata = new GameMetadata
            {
                Id = 1,
                IgdbId = 12345,
                SteamAppId = 67890,
                Title = "Test Game",
                Year = 2023,
                CleanTitle = "testgame",
                SortTitle = "test game"
            };

            _newGame = new Game
            {
                IgdbId = 12345,
                SteamAppId = 67890,
                Title = "Test Game",
                RootFolderPath = "/games",
                QualityProfileId = 1,
                Monitored = true
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfo(It.IsAny<int>()))
                  .Returns(_gameMetadata);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameBySteamAppId(It.IsAny<int>()))
                  .Returns(_gameMetadata);

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetGameFolder(It.IsAny<Game>()))
                  .Returns("Test Game (2023)");

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.IsAny<Game>()))
                  .Returns(new ValidationResult());

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AddGame(It.IsAny<Game>()))
                  .Returns<Game>(g => g);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AddGames(It.IsAny<List<Game>>()))
                  .Returns<List<Game>>(g => g);
        }

        [Test]
        public void should_add_game_with_igdb_id()
        {
            var result = Subject.AddGame(_newGame);

            result.Should().NotBeNull();
            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(s => s.GetGameInfo(_newGame.IgdbId), Times.Once());
        }

        [Test]
        public void should_add_game_with_steam_id_only()
        {
            _newGame.IgdbId = 0;

            var result = Subject.AddGame(_newGame);

            result.Should().NotBeNull();
            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(s => s.GetGameBySteamAppId(_newGame.SteamAppId), Times.Once());
        }

        [Test]
        public void should_set_path_when_not_provided()
        {
            _newGame.Path = null;

            var result = Subject.AddGame(_newGame);

            result.Path.Should().Be("/games/Test Game (2023)");
        }

        [Test]
        public void should_throw_when_validation_fails()
        {
            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.IsAny<Game>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                  {
                      new ValidationFailure("Path", "Path is invalid")
                  }));

            Assert.Throws<ValidationException>(() => Subject.AddGame(_newGame));
        }

        [Test]
        public void should_throw_when_game_not_found()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfo(It.IsAny<int>()))
                  .Throws(new Exceptions.GameNotFoundException(12345));

            Assert.Throws<ValidationException>(() => Subject.AddGame(_newGame));
        }

        [Test]
        public void should_add_multiple_games()
        {
            var games = new List<Game>
            {
                new Game { IgdbId = 111, RootFolderPath = "/games" },
                new Game { IgdbId = 222, RootFolderPath = "/games" }
            };

            var result = Subject.AddGames(games);

            result.Should().HaveCount(2);
        }

        [Test]
        public void should_ignore_errors_when_flag_is_set()
        {
            var games = new List<Game>
            {
                new Game { IgdbId = 111, RootFolderPath = "/games" },
                new Game { IgdbId = 222, RootFolderPath = "/games" }
            };

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.Is<Game>(g => g.IgdbId == 111)))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                  {
                      new ValidationFailure("Path", "Path is invalid")
                  }));

            var result = Subject.AddGames(games, true);

            result.Should().HaveCount(1);
            result[0].IgdbId.Should().Be(222);
        }

        [Test]
        public void should_throw_on_error_when_ignore_flag_is_false()
        {
            var games = new List<Game>
            {
                new Game { IgdbId = 111, RootFolderPath = "/games" }
            };

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.IsAny<Game>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                  {
                      new ValidationFailure("Path", "Path is invalid")
                  }));

            Assert.Throws<ValidationException>(() => Subject.AddGames(games, false));
        }

        [Test]
        public void should_set_added_date()
        {
            var beforeAdd = DateTime.UtcNow.AddSeconds(-1);

            var result = Subject.AddGame(_newGame);

            result.Added.Should().BeAfter(beforeAdd);
            result.Added.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        }

        [Test]
        public void should_upsert_game_metadata()
        {
            Subject.AddGame(_newGame);

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(s => s.Upsert(It.IsAny<GameMetadata>()), Times.Once());
        }

        [Test]
        public void should_update_tags()
        {
            Subject.AddGame(_newGame);

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.UpdateTags(It.IsAny<Game>()), Times.Once());
        }
    }
}
