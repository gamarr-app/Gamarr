using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Games;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games
{
    [TestFixture]
    public class AddGameServiceFixture : CoreTest<AddGameService>
    {
        private Game _newGame;
        private GameMetadata _metadata;

        [SetUp]
        public void Setup()
        {
            _metadata = new GameMetadata
            {
                Id = 1,
                IgdbId = 100,
                SteamAppId = 200,
                Title = "Test Game",
                CleanTitle = "testgame",
                SortTitle = "test game",
                IgdbSlug = "test-game"
            };

            _newGame = new Game
            {
                IgdbId = 100,
                RootFolderPath = "/games/",
                QualityProfileId = 1,
                Monitored = true,
                Tags = new HashSet<int>(),
                GameMetadata = new LazyLoaded<GameMetadata>(_metadata)
            };

            Mocker.GetMock<IProvideGameInfo>()
                .Setup(s => s.GetGameInfoByIgdbId(It.IsAny<int>()))
                .Returns(_metadata);

            Mocker.GetMock<IBuildFileNames>()
                .Setup(s => s.GetGameFolder(It.IsAny<Game>(), It.IsAny<NamingConfig>()))
                .Returns("Test Game (2024)");

            Mocker.GetMock<IAddGameValidator>()
                .Setup(s => s.Validate(It.IsAny<Game>()))
                .Returns(new ValidationResult());

            Mocker.GetMock<IGameService>()
                .Setup(s => s.AddGame(It.IsAny<Game>()))
                .Returns<Game>(g => g);

            Mocker.GetMock<IGameMetadataService>()
                .Setup(s => s.Upsert(It.IsAny<GameMetadata>()))
                .Returns(true);
        }

        [Test]
        public void should_add_game_and_return_it()
        {
            var result = Subject.AddGame(_newGame);

            result.Should().NotBeNull();

            Mocker.GetMock<IGameService>()
                .Verify(s => s.AddGame(It.IsAny<Game>()), Times.Once());
        }

        [Test]
        public void should_fetch_metadata_from_igdb()
        {
            Subject.AddGame(_newGame);

            Mocker.GetMock<IProvideGameInfo>()
                .Verify(s => s.GetGameInfoByIgdbId(100), Times.Once());
        }

        [Test]
        public void should_upsert_game_metadata()
        {
            Subject.AddGame(_newGame);

            Mocker.GetMock<IGameMetadataService>()
                .Verify(s => s.Upsert(It.IsAny<GameMetadata>()), Times.Once());
        }

        [Test]
        public void should_set_path_from_root_folder_and_folder_name()
        {
            _newGame.Path = null;

            var result = Subject.AddGame(_newGame);

            result.Path.Should().Contain("Test Game (2024)");
        }

        [Test]
        public void should_use_existing_path_when_provided()
        {
            _newGame.Path = "/custom/path";

            var result = Subject.AddGame(_newGame);

            result.Path.Should().Be("/custom/path");
        }

        [Test]
        public void should_validate_game_before_adding()
        {
            Subject.AddGame(_newGame);

            Mocker.GetMock<IAddGameValidator>()
                .Verify(s => s.Validate(It.IsAny<Game>()), Times.Once());
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

            Mocker.GetMock<IGameService>()
                .Verify(s => s.AddGame(It.IsAny<Game>()), Times.Never());
        }

        [Test]
        public void should_throw_when_igdb_game_not_found()
        {
            Mocker.GetMock<IProvideGameInfo>()
                .Setup(s => s.GetGameInfoByIgdbId(It.IsAny<int>()))
                .Throws(new GameNotFoundException(100));

            Assert.Throws<ValidationException>(() => Subject.AddGame(_newGame));
        }

        [Test]
        public void should_update_tags_before_adding()
        {
            Subject.AddGame(_newGame);

            Mocker.GetMock<IGameService>()
                .Verify(s => s.UpdateTags(It.IsAny<Game>()), Times.Once());
        }

        [Test]
        public void should_fetch_by_steam_id_when_no_igdb_id()
        {
            _newGame.IgdbId = 0;
            _newGame.SteamAppId = 200;

            Mocker.GetMock<IProvideGameInfo>()
                .Setup(s => s.GetGameBySteamAppId(200))
                .Returns(_metadata);

            Subject.AddGame(_newGame);

            Mocker.GetMock<IProvideGameInfo>()
                .Verify(s => s.GetGameBySteamAppId(200), Times.Once());

            Mocker.GetMock<IProvideGameInfo>()
                .Verify(s => s.GetGameInfoByIgdbId(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_throw_when_steam_game_not_found()
        {
            _newGame.IgdbId = 0;
            _newGame.SteamAppId = 999;

            Mocker.GetMock<IProvideGameInfo>()
                .Setup(s => s.GetGameBySteamAppId(999))
                .Returns((GameMetadata)null);

            Assert.Throws<ValidationException>(() => Subject.AddGame(_newGame));
        }

        [Test]
        public void should_add_multiple_games()
        {
            var games = new List<Game>
            {
                _newGame,
                new Game
                {
                    IgdbId = 200,
                    RootFolderPath = "/games/",
                    QualityProfileId = 1,
                    Tags = new HashSet<int>(),
                    GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                    {
                        IgdbId = 200,
                        Title = "Second Game",
                        IgdbSlug = "second-game"
                    })
                }
            };

            Mocker.GetMock<IGameService>()
                .Setup(s => s.AddGames(It.IsAny<List<Game>>()))
                .Returns<List<Game>>(g => g);

            var result = Subject.AddGames(games);

            result.Should().HaveCount(2);

            Mocker.GetMock<IGameMetadataService>()
                .Verify(s => s.UpsertMany(It.Is<List<GameMetadata>>(l => l.Count == 2)), Times.Once());
        }

        [Test]
        public void should_skip_invalid_games_when_ignoring_errors()
        {
            var invalidGame = new Game
            {
                IgdbId = 999,
                RootFolderPath = "/games/",
                Tags = new HashSet<int>(),
                GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata { IgdbId = 999 })
            };

            Mocker.GetMock<IProvideGameInfo>()
                .Setup(s => s.GetGameInfoByIgdbId(999))
                .Throws(new GameNotFoundException(999));

            Mocker.GetMock<IGameService>()
                .Setup(s => s.AddGames(It.IsAny<List<Game>>()))
                .Returns<List<Game>>(g => g);

            var result = Subject.AddGames(new List<Game> { _newGame, invalidGame }, ignoreErrors: true);

            result.Should().HaveCount(1);
        }

        [Test]
        public void should_set_added_date()
        {
            var result = Subject.AddGame(_newGame);

            result.Added.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
