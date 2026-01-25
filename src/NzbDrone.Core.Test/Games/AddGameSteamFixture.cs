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
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Games
{
    [TestFixture]
    public class AddGameSteamFixture : CoreTest<AddGameService>
    {
        private Game _newGame;
        private GameMetadata _steamMetadata;

        [SetUp]
        public void Setup()
        {
            _newGame = new Game
            {
                GameMetadata = new GameMetadata
                {
                    SteamAppId = 570,
                    IgdbId = 0,
                    Title = "Dota 2",
                    CleanTitle = "dota2",
                    SortTitle = "dota 2"
                },
                RootFolderPath = "/games",
                QualityProfileId = 1,
                Monitored = true,
                Tags = new HashSet<int>()
            };

            _steamMetadata = new GameMetadata
            {
                SteamAppId = 570,
                IgdbId = 5000,
                Title = "Dota 2",
                CleanTitle = "dota2",
                SortTitle = "dota 2",
                Year = 2013,
                Status = GameStatusType.Released,
                GameType = GameType.MainGame
            };

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.IsAny<Game>()))
                  .Returns(new ValidationResult());

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetGameFolder(It.IsAny<Game>(), It.IsAny<NamingConfig>()))
                  .Returns("Dota 2 (2013)");

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AddGame(It.IsAny<Game>()))
                  .Returns<Game>(g => g);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.UpdateTags(It.IsAny<Game>()))
                  .Returns(false);
        }

        [Test]
        public void should_add_game_using_steam_app_id_when_igdb_id_is_zero()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameBySteamAppId(570))
                  .Returns(_steamMetadata);

            Subject.AddGame(_newGame);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameBySteamAppId(570), Times.Once());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameInfo(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_add_game_using_igdb_id_when_available()
        {
            var gameWithIgdb = new Game
            {
                GameMetadata = new GameMetadata
                {
                    SteamAppId = 570,
                    IgdbId = 5000,
                    Title = "Dota 2",
                    CleanTitle = "dota2",
                    SortTitle = "dota 2"
                },
                RootFolderPath = "/games",
                QualityProfileId = 1,
                Monitored = true,
                Tags = new HashSet<int>()
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfo(5000))
                  .Returns(_steamMetadata);

            Subject.AddGame(gameWithIgdb);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameInfo(5000), Times.Once());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameBySteamAppId(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_handle_batch_add_with_some_failures_gracefully()
        {
            var validGame = new Game
            {
                GameMetadata = new GameMetadata
                {
                    SteamAppId = 570,
                    IgdbId = 5000,
                    Title = "Valid Game",
                    CleanTitle = "validgame",
                    SortTitle = "valid game"
                },
                RootFolderPath = "/games",
                QualityProfileId = 1,
                Monitored = true,
                Tags = new HashSet<int>()
            };

            var invalidGame = new Game
            {
                GameMetadata = new GameMetadata
                {
                    SteamAppId = 0,
                    IgdbId = 9999,
                    Title = "Invalid Game",
                    CleanTitle = "invalidgame",
                    SortTitle = "invalid game"
                },
                RootFolderPath = "/games",
                QualityProfileId = 1,
                Monitored = true,
                Tags = new HashSet<int>()
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfo(5000))
                  .Returns(_steamMetadata);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfo(9999))
                  .Throws(new Exceptions.GameNotFoundException(9999));

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.Is<Game>(g => g.GameMetadata.Value.Title == "Invalid Game")))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                  {
                      new ValidationFailure("IgdbId", "Game not found")
                  }));

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AddGames(It.IsAny<List<Game>>()))
                  .Returns<List<Game>>(g => g);

            var result = Subject.AddGames(new List<Game> { validGame, invalidGame }, true);

            result.Should().HaveCount(1);

            ExceptionVerification.IgnoreErrors();
        }

        [Test]
        public void should_throw_validation_exception_when_validation_fails()
        {
            var invalidGame = new Game
            {
                GameMetadata = new GameMetadata
                {
                    SteamAppId = 570,
                    IgdbId = 0,
                    Title = "Invalid Game",
                    CleanTitle = "invalidgame",
                    SortTitle = "invalid game"
                },
                RootFolderPath = "/games",
                QualityProfileId = 1,
                Tags = new HashSet<int>()
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameBySteamAppId(570))
                  .Returns(_steamMetadata);

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.IsAny<Game>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                  {
                      new ValidationFailure("Path", "Path is not valid")
                  }));

            Assert.Throws<ValidationException>(() => Subject.AddGame(invalidGame));
        }

        [Test]
        public void should_enrich_metadata_from_provider()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameBySteamAppId(570))
                  .Returns(_steamMetadata);

            var result = Subject.AddGame(_newGame);

            result.GameMetadata.Value.IgdbId.Should().Be(5000);
            result.GameMetadata.Value.SteamAppId.Should().Be(570);

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.SteamAppId == 570 && m.IgdbId == 5000)), Times.Once());
        }

        [Test]
        public void should_throw_validation_exception_when_steam_provider_returns_null()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameBySteamAppId(570))
                  .Returns((GameMetadata)null);

            Assert.Throws<ValidationException>(() => Subject.AddGame(_newGame));

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.AddGame(It.IsAny<Game>()), Times.Never());
        }

        [Test]
        public void should_set_path_from_root_folder_when_path_is_empty()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameBySteamAppId(570))
                  .Returns(_steamMetadata);

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetGameFolder(It.IsAny<Game>(), It.IsAny<NamingConfig>()))
                  .Returns("Dota 2 (2013)");

            var result = Subject.AddGame(_newGame);

            result.Path.Should().Be("/games/Dota 2 (2013)".AsOsAgnostic());
        }

        [Test]
        public void should_use_steam_app_id_from_metadata_when_game_steam_id_is_zero()
        {
            var gameWithMetadataSteamId = new Game
            {
                GameMetadata = new GameMetadata
                {
                    SteamAppId = 730,
                    IgdbId = 0,
                    Title = "CS2",
                    CleanTitle = "cs2",
                    SortTitle = "cs2"
                },
                RootFolderPath = "/games",
                QualityProfileId = 1,
                Monitored = true,
                Tags = new HashSet<int>()
            };

            var cs2Metadata = new GameMetadata
            {
                SteamAppId = 730,
                IgdbId = 6000,
                Title = "Counter-Strike 2",
                CleanTitle = "counterstrike2",
                SortTitle = "counter-strike 2",
                Year = 2023,
                Status = GameStatusType.Released,
                GameType = GameType.MainGame
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameBySteamAppId(730))
                  .Returns(cs2Metadata);

            Subject.AddGame(gameWithMetadataSteamId);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameBySteamAppId(730), Times.Once());
        }

        [Test]
        public void should_upsert_metadata_before_adding_game()
        {
            var callOrder = new List<string>();

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameBySteamAppId(570))
                  .Returns(_steamMetadata);

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.Upsert(It.IsAny<GameMetadata>()))
                  .Callback<GameMetadata>(m => callOrder.Add("upsert"));

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AddGame(It.IsAny<Game>()))
                  .Callback<Game>(g => callOrder.Add("add"))
                  .Returns<Game>(g => g);

            Subject.AddGame(_newGame);

            callOrder.Should().ContainInOrder("upsert", "add");
        }
    }
}
