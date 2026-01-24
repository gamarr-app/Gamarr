using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class GamesWithoutMetadataCheckFixture : CoreTest<GamesWithoutMetadataCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                  .Returns("Some Warning Message");
        }

        private void GivenGames(List<Game> games)
        {
            Mocker.GetMock<IGameService>()
                .Setup(v => v.GetAllGames())
                .Returns(games);
        }

        [Test]
        public void should_return_ok_when_all_games_have_metadata()
        {
            GivenGames(new List<Game>
            {
                new Game
                {
                    Id = 1,
                    Title = "Half-Life 2",
                    GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                    {
                        Overview = "A first-person shooter game",
                        Genres = new List<string> { "FPS" },
                        Year = 2004
                    })
                }
            });

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_warning_when_game_has_no_metadata()
        {
            GivenGames(new List<Game>
            {
                new Game
                {
                    Id = 1,
                    Title = "Unknown Game",
                    GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                    {
                        Overview = null,
                        Genres = new List<string>(),
                        Year = 0
                    })
                }
            });

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_return_ok_when_game_has_overview_only()
        {
            GivenGames(new List<Game>
            {
                new Game
                {
                    Id = 1,
                    Title = "Game With Overview",
                    GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                    {
                        Overview = "This game has an overview",
                        Genres = new List<string>(),
                        Year = 0
                    })
                }
            });

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_ok_when_game_has_genres_only()
        {
            GivenGames(new List<Game>
            {
                new Game
                {
                    Id = 1,
                    Title = "Game With Genres",
                    GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                    {
                        Overview = null,
                        Genres = new List<string> { "Action" },
                        Year = 0
                    })
                }
            });

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_ok_when_game_has_year_only()
        {
            GivenGames(new List<Game>
            {
                new Game
                {
                    Id = 1,
                    Title = "Game With Year",
                    GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                    {
                        Overview = null,
                        Genres = new List<string>(),
                        Year = 2023
                    })
                }
            });

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_ok_when_no_games_exist()
        {
            GivenGames(new List<Game>());

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_warning_with_multiple_games_without_metadata()
        {
            GivenGames(new List<Game>
            {
                new Game
                {
                    Id = 1,
                    Title = "Game 1",
                    GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                    {
                        Overview = null,
                        Genres = new List<string>(),
                        Year = 0
                    })
                },
                new Game
                {
                    Id = 2,
                    Title = "Game 2",
                    GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                    {
                        Overview = "",
                        Genres = null,
                        Year = 0
                    })
                }
            });

            Subject.Check().ShouldBeWarning();
        }
    }
}
