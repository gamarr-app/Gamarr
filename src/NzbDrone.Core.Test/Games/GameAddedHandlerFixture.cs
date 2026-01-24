using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games
{
    [TestFixture]
    public class GameAddedHandlerFixture : CoreTest<GameAddedHandler>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 5,
                GameMetadata = new GameMetadata { Title = "Test Game" }
            };
        }

        [Test]
        public void should_queue_refresh_game_command_when_game_is_added()
        {
            Subject.Handle(new GameAddedEvent(_game));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push(
                      It.Is<RefreshGameCommand>(c => c.GameIds.Contains(_game.Id) && c.IsNewGame == true),
                      CommandPriority.Normal,
                      CommandTrigger.Unspecified), Times.Once());
        }

        [Test]
        public void should_use_correct_game_id_in_command()
        {
            _game.Id = 42;

            Subject.Handle(new GameAddedEvent(_game));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push(
                      It.Is<RefreshGameCommand>(c => c.GameIds.Count == 1 && c.GameIds[0] == 42),
                      CommandPriority.Normal,
                      CommandTrigger.Unspecified), Times.Once());
        }

        [Test]
        public void should_push_many_commands_when_games_imported()
        {
            var games = new List<Game>
            {
                new Game { Id = 1, GameMetadata = new GameMetadata { Title = "Game 1" } },
                new Game { Id = 2, GameMetadata = new GameMetadata { Title = "Game 2" } },
                new Game { Id = 3, GameMetadata = new GameMetadata { Title = "Game 3" } }
            };

            Subject.Handle(new GamesImportedEvent(games));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.PushMany(It.Is<List<RefreshGameCommand>>(c => c.Count == 3)), Times.Once());
        }

        [Test]
        public void should_use_correct_game_ids_in_imported_commands()
        {
            var games = new List<Game>
            {
                new Game { Id = 10, GameMetadata = new GameMetadata { Title = "Game A" } },
                new Game { Id = 20, GameMetadata = new GameMetadata { Title = "Game B" } }
            };

            Subject.Handle(new GamesImportedEvent(games));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.PushMany(It.Is<List<RefreshGameCommand>>(c =>
                      c[0].GameIds[0] == 10 &&
                      c[1].GameIds[0] == 20 &&
                      c[0].IsNewGame == true &&
                      c[1].IsNewGame == true)), Times.Once());
        }
    }
}
