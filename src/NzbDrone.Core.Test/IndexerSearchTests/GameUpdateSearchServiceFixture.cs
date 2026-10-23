using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class GameUpdateSearchServiceFixture : CoreTest<GameUpdateSearchService>
    {
        private Game _game;
        private GameFile _baseFile;
        private List<DownloadDecision> _decisions;

        [SetUp]
        public void SetUp()
        {
            _game = new Game
            {
                Id = 7,
                Monitored = true,
                MonitorUpdates = true,
                GameMetadata = new GameMetadata { Title = "Test Game" }
            };

            _baseFile = new GameFile { Id = 1, GameId = _game.Id, RelativePath = string.Empty, Game = _game };

            _decisions = new List<DownloadDecision>();

            Mocker.GetMock<IGameService>()
                .Setup(s => s.FindGame(_game.Id))
                .Returns(_game);

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByGame(It.IsAny<int>()))
                .Returns(() => new List<GameFile> { _baseFile });

            Mocker.GetMock<IGameComponentService>()
                .Setup(s => s.GetByGame(It.IsAny<int>()))
                .Returns(new List<GameComponent>());

            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.GameSearch(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(() => Task.FromResult(_decisions));

            Mocker.GetMock<IProcessDownloadDecisions>()
                .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                .Returns(Task.FromResult(new ProcessedDecisions(new List<DownloadDecision>(),
                                                                new List<DownloadDecision>(),
                                                                new List<DownloadDecision>())));
        }

        private static DownloadDecision Decision(string title, ReleaseContentType contentType)
        {
            return new DownloadDecision(new RemoteGame
            {
                Release = new ReleaseInfo { Title = title },
                ParsedGameInfo = new ParsedGameInfo { ContentType = contentType }
            });
        }

        private void VerifyQueued(Times times)
        {
            Mocker.GetMock<IManageCommandQueue>()
                .Verify(v => v.Push(It.IsAny<GameUpdateSearchCommand>(),
                                    It.IsAny<CommandPriority>(),
                                    It.IsAny<CommandTrigger>()), times);
        }

        private void VerifySearched(int gameId, Times times)
        {
            Mocker.GetMock<ISearchForReleases>()
                .Verify(s => s.GameSearch(gameId, It.IsAny<bool>(), It.IsAny<bool>()), times);
        }

        [Test]
        public void should_queue_an_update_search_when_a_base_file_is_imported()
        {
            Subject.Handle(new GameFileAddedEvent(_baseFile));

            VerifyQueued(Times.Once());
        }

        // Manual import and disk-scan adoption both land here with no Game
        // attached to the file, so the game has to be resolved by id.
        [Test]
        public void should_resolve_the_game_when_the_added_file_has_none_attached()
        {
            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 2, GameId = _game.Id, RelativePath = string.Empty }));

            VerifyQueued(Times.Once());
        }

        [Test]
        public void should_not_queue_a_search_for_an_update_only_import()
        {
            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 3, GameId = _game.Id, RelativePath = "Updates/v1.0.2", Game = _game }));

            VerifyQueued(Times.Never());
        }

        [Test]
        public void should_not_queue_a_search_for_a_dlc_import()
        {
            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 4, GameId = _game.Id, RelativePath = "DLC/The Blood Price", Game = _game }));

            VerifyQueued(Times.Never());
        }

        [Test]
        public void should_not_queue_a_search_for_an_unmonitored_game()
        {
            _game.Monitored = false;

            Subject.Handle(new GameFileAddedEvent(_baseFile));

            VerifyQueued(Times.Never());
        }

        [Test]
        public void should_not_queue_a_search_when_updates_are_not_monitored()
        {
            _game.MonitorUpdates = false;

            Subject.Handle(new GameFileAddedEvent(_baseFile));

            VerifyQueued(Times.Never());
        }

        [Test]
        public void should_not_queue_a_search_for_a_game_that_was_deleted()
        {
            Mocker.GetMock<IGameService>()
                .Setup(s => s.FindGame(_game.Id))
                .Returns((Game)null);

            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 5, GameId = _game.Id, RelativePath = string.Empty }));

            VerifyQueued(Times.Never());
        }

        // A release that imports as several files publishes one event each; the
        // game is worth a single search however many files it brought.
        [Test]
        public void should_queue_one_search_for_a_multi_file_import()
        {
            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 6, GameId = _game.Id, RelativePath = "disc1.iso", Game = _game }));
            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 7, GameId = _game.Id, RelativePath = "disc2.iso", Game = _game }));
            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 8, GameId = _game.Id, RelativePath = "disc3.iso", Game = _game }));

            VerifyQueued(Times.Once());
        }

        [Test]
        public void should_search_the_game_the_import_queued()
        {
            Subject.Handle(new GameFileAddedEvent(_baseFile));
            Subject.Execute(new GameUpdateSearchCommand());

            VerifySearched(_game.Id, Times.Once());
        }

        // One command drains everything a scan queued, one game at a time,
        // rather than one command (and one executor thread) per game.
        [Test]
        public void should_drain_every_game_a_scan_queued_with_one_command()
        {
            var second = new Game { Id = 9, Monitored = true, MonitorUpdates = true, GameMetadata = new GameMetadata { Title = "Second" } };

            Mocker.GetMock<IGameService>().Setup(s => s.FindGame(second.Id)).Returns(second);

            Subject.Handle(new GameFileAddedEvent(_baseFile));
            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 10, GameId = second.Id, RelativePath = string.Empty, Game = second }));

            Subject.Execute(new GameUpdateSearchCommand());

            VerifySearched(_game.Id, Times.Once());
            VerifySearched(second.Id, Times.Once());
        }

        // The queue can sit for a while, so the captured id is not to be
        // trusted: the game is resolved again when the command runs.
        [Test]
        public void should_not_search_a_game_deleted_before_the_command_ran()
        {
            Subject.Handle(new GameFileAddedEvent(_baseFile));

            Mocker.GetMock<IGameService>()
                .Setup(s => s.FindGame(_game.Id))
                .Returns((Game)null);

            Subject.Execute(new GameUpdateSearchCommand());

            VerifySearched(_game.Id, Times.Never());
        }

        [Test]
        public void should_not_search_when_updates_were_turned_off_before_the_command_ran()
        {
            Subject.Handle(new GameFileAddedEvent(_baseFile));

            _game.MonitorUpdates = false;

            Subject.Execute(new GameUpdateSearchCommand());

            VerifySearched(_game.Id, Times.Never());
        }

        // Without a base file on disk every update release can only be
        // rejected, so there is nothing to search for yet.
        [Test]
        public void should_not_search_when_only_an_update_file_is_on_disk()
        {
            Subject.Handle(new GameFileAddedEvent(_baseFile));

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByGame(_game.Id))
                .Returns(new List<GameFile> { new GameFile { Id = 11, GameId = _game.Id, RelativePath = "Updates/v1.0.1" } });

            Subject.Execute(new GameUpdateSearchCommand());

            VerifySearched(_game.Id, Times.Never());
        }

        // The base game is already on disk; handing its releases back to the
        // processor could only re-grab the file that was just imported.
        [Test]
        public void should_process_only_the_update_releases()
        {
            _decisions = new List<DownloadDecision>
            {
                Decision("Test.Game-RAZOR", ReleaseContentType.BaseGame),
                Decision("Test.Game.Update.v1.0.1-GRP", ReleaseContentType.UpdateOnly),
                Decision("Test.Game.Update.v1.0.2-GRP", ReleaseContentType.UpdateOnly),
                Decision("Test.Game.Blood.Price.DLC-GRP", ReleaseContentType.DlcOnly)
            };

            List<DownloadDecision> processed = null;

            Mocker.GetMock<IProcessDownloadDecisions>()
                .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                .Callback<List<DownloadDecision>>(d => processed = d)
                .Returns(Task.FromResult(new ProcessedDecisions(new List<DownloadDecision>(),
                                                                new List<DownloadDecision>(),
                                                                new List<DownloadDecision>())));

            Subject.Handle(new GameFileAddedEvent(_baseFile));
            Subject.Execute(new GameUpdateSearchCommand());

            processed.Should().NotBeNull();
            processed.Select(d => d.RemoteGame.Release.Title)
                     .Should().BeEquivalentTo("Test.Game.Update.v1.0.1-GRP", "Test.Game.Update.v1.0.2-GRP");
        }

        [Test]
        public void should_queue_a_component_search_for_a_monitored_dlc_slot_without_a_file()
        {
            Mocker.GetMock<IGameComponentService>()
                .Setup(s => s.GetByGame(_game.Id))
                .Returns(new List<GameComponent>
                {
                    new GameComponent { Id = 20, GameId = _game.Id, ComponentType = GameComponentType.Base, Key = "base", Monitored = true },
                    new GameComponent { Id = 21, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:111", Title = "The Blood Price", Monitored = true },
                    new GameComponent { Id = 22, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:222", Title = "Warm Winds", Monitored = false }
                });

            Subject.Handle(new GameFileAddedEvent(_baseFile));
            Subject.Execute(new GameUpdateSearchCommand());

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(v => v.Push(It.Is<GameComponentSearchCommand>(c => c.ComponentId == 21),
                                    It.IsAny<CommandPriority>(),
                                    It.IsAny<CommandTrigger>()), Times.Once());

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(v => v.Push(It.Is<GameComponentSearchCommand>(c => c.ComponentId == 22),
                                    It.IsAny<CommandPriority>(),
                                    It.IsAny<CommandTrigger>()), Times.Never());
        }

        [Test]
        public void should_not_queue_a_component_search_for_a_dlc_slot_that_has_a_file()
        {
            Mocker.GetMock<IGameComponentService>()
                .Setup(s => s.GetByGame(_game.Id))
                .Returns(new List<GameComponent>
                {
                    new GameComponent { Id = 21, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:111", Title = "The Blood Price", Monitored = true }
                });

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByGame(_game.Id))
                .Returns(new List<GameFile>
                {
                    _baseFile,
                    new GameFile { Id = 12, GameId = _game.Id, RelativePath = "DLC/The Blood Price", ComponentId = 21 }
                });

            Subject.Handle(new GameFileAddedEvent(_baseFile));
            Subject.Execute(new GameUpdateSearchCommand());

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(v => v.Push(It.IsAny<GameComponentSearchCommand>(),
                                    It.IsAny<CommandPriority>(),
                                    It.IsAny<CommandTrigger>()), Times.Never());
        }

        // Backing off a game whose searches keep coming back empty is the only
        // thing standing between a churning library and the indexer quota.
        [Test]
        public void should_respect_the_search_backoff()
        {
            Mocker.GetMock<IGameSearchBackoff>()
                .Setup(s => s.ShouldSkip(It.IsAny<Game>()))
                .Returns(true);

            Subject.Handle(new GameFileAddedEvent(_baseFile));
            Subject.Execute(new GameUpdateSearchCommand());

            VerifySearched(_game.Id, Times.Never());
        }
    }
}
