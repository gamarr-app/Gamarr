using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games
{
    [TestFixture]
    public class GameScannedHandlerFixture : CoreTest<GameScannedHandler>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 5,
                GameMetadata = new GameMetadata
                {
                    Title = "Test Game",
                    CollectionIgdbId = 0
                }
            };
        }

        [Test]
        public void should_trigger_search_when_search_for_game_is_true()
        {
            _game.AddOptions = new AddGameOptions
            {
                SearchForGame = true,
                Monitor = MonitorTypes.GameOnly
            };

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push(
                      It.Is<GamesSearchCommand>(c => c.GameIds.Contains(_game.Id)),
                      CommandPriority.Normal,
                      CommandTrigger.Unspecified), Times.Once());
        }

        [Test]
        public void should_not_trigger_search_when_add_options_is_null()
        {
            _game.AddOptions = null;

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push(
                      It.IsAny<GamesSearchCommand>(),
                      It.IsAny<CommandPriority>(),
                      It.IsAny<CommandTrigger>()), Times.Never());
        }

        [Test]
        public void should_not_trigger_search_when_search_for_game_is_false()
        {
            _game.AddOptions = new AddGameOptions
            {
                SearchForGame = false,
                Monitor = MonitorTypes.GameOnly
            };

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push(
                      It.IsAny<GamesSearchCommand>(),
                      It.IsAny<CommandPriority>(),
                      It.IsAny<CommandTrigger>()), Times.Never());
        }

        [Test]
        public void should_remove_add_options_after_processing()
        {
            _game.AddOptions = new AddGameOptions
            {
                SearchForGame = false,
                Monitor = MonitorTypes.GameOnly
            };

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.RemoveAddOptions(_game), Times.Once());
        }

        [Test]
        public void should_monitor_collection_if_game_has_collection_and_monitor_type_is_game_and_collection()
        {
            var collection = new GameCollection
            {
                Id = 10,
                IgdbId = 99,
                Monitored = false
            };

            _game.GameMetadata.Value.CollectionIgdbId = 99;
            _game.AddOptions = new AddGameOptions
            {
                SearchForGame = false,
                Monitor = MonitorTypes.GameAndCollection
            };

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.FindByIgdbId(99))
                  .Returns(collection);

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.UpdateCollection(It.Is<GameCollection>(c => c.Monitored == true)), Times.Once());
        }

        [Test]
        public void should_not_monitor_collection_when_game_has_no_collection()
        {
            _game.GameMetadata.Value.CollectionIgdbId = 0;
            _game.AddOptions = new AddGameOptions
            {
                SearchForGame = false,
                Monitor = MonitorTypes.GameAndCollection
            };

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.FindByIgdbId(It.IsAny<int>()), Times.Never());

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.UpdateCollection(It.IsAny<GameCollection>()), Times.Never());
        }

        [Test]
        public void should_not_monitor_collection_when_monitor_type_is_game_only()
        {
            _game.GameMetadata.Value.CollectionIgdbId = 99;
            _game.AddOptions = new AddGameOptions
            {
                SearchForGame = false,
                Monitor = MonitorTypes.GameOnly
            };

            Subject.Handle(new GameScannedEvent(_game, new List<string>()));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.FindByIgdbId(It.IsAny<int>()), Times.Never());

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.UpdateCollection(It.IsAny<GameCollection>()), Times.Never());
        }

        [Test]
        public void should_handle_game_scan_skipped_event()
        {
            _game.AddOptions = new AddGameOptions
            {
                SearchForGame = true,
                Monitor = MonitorTypes.GameOnly
            };

            Subject.Handle(new GameScanSkippedEvent(_game, GameScanSkippedReason.RootFolderDoesNotExist));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push(
                      It.Is<GamesSearchCommand>(c => c.GameIds.Contains(_game.Id)),
                      CommandPriority.Normal,
                      CommandTrigger.Unspecified), Times.Once());
        }
    }
}
