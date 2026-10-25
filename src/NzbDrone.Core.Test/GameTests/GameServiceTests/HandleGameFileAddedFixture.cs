using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests.GameServiceTests
{
    [TestFixture]
    public class HandleGameFileAddedFixture : CoreTest<GameService>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                 .With(g => g.Id = 31)
                                 .With(g => g.GameFileId = 0)
                                 .Build();

            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.Update(It.IsAny<Game>()))
                  .Returns<Game>(g => g);
        }

        private void GivenStoredGameFileId(int gameFileId)
        {
            var stored = Builder<Game>.CreateNew()
                                      .With(g => g.Id = _game.Id)
                                      .With(g => g.GameFileId = gameFileId)
                                      .Build();

            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.Find(_game.Id))
                  .Returns(stored);
        }

        private GameFileAddedEvent GivenAddedFile(int id, string relativePath)
        {
            return new GameFileAddedEvent(new GameFile
            {
                Id = id,
                GameId = _game.Id,
                RelativePath = relativePath,
                Game = _game
            });
        }

        [Test]
        public void should_assign_base_import_as_primary()
        {
            GivenStoredGameFileId(0);

            Subject.Handle(GivenAddedFile(24, string.Empty));

            _game.GameFileId.Should().Be(24);
            Mocker.GetMock<IGameRepository>().Verify(v => v.Update(_game), Times.Once());
        }

        [Test]
        public void should_not_displace_primary_with_a_subfolder_unit()
        {
            GivenStoredGameFileId(23);

            Subject.Handle(GivenAddedFile(24, "Updates/v1.5"));

            _game.GameFileId.Should().Be(0);
            Mocker.GetMock<IGameRepository>().Verify(v => v.Update(It.IsAny<Game>()), Times.Never());
        }

        [Test]
        public void should_assign_primary_when_attached_game_still_points_at_the_replaced_file()
        {
            // The upgrade deleted file 23 and cleared GameFileId on the row, but
            // the Game the importer carries was loaded before that.
            _game.GameFileId = 23;
            GivenStoredGameFileId(0);

            Subject.Handle(GivenAddedFile(24, "Songs.of.Syx.exe"));

            _game.GameFileId.Should().Be(24);
            Mocker.GetMock<IGameRepository>().Verify(v => v.Update(_game), Times.Once());
        }

        [Test]
        public void should_not_throw_when_the_game_was_deleted_mid_import()
        {
            var message = new GameFileAddedEvent(new GameFile { Id = 24, GameId = _game.Id, RelativePath = string.Empty });

            Subject.Handle(message);

            Mocker.GetMock<IGameRepository>().Verify(v => v.Update(It.IsAny<Game>()), Times.Never());
        }

        // #216: handlers that react to an import read the primary-file pointer
        // this handler writes, so it has to run before all of them — the
        // post-import update search was queued first and saw GameFileId 0.
        [Test]
        public void should_assign_the_primary_file_before_any_other_handler_runs()
        {
            var order = typeof(GameService).GetMethod(nameof(GameService.Handle), new[] { typeof(GameFileAddedEvent) })
                                           .GetCustomAttributes(typeof(EventHandleOrderAttribute), true)
                                           .Cast<EventHandleOrderAttribute>()
                                           .SingleOrDefault();

            order.Should().NotBeNull();
            order.EventHandleOrder.Should().Be(EventHandleOrder.First);
        }

        [Test]
        public void should_queue_the_post_import_update_search_after_the_assignment()
        {
            var order = typeof(IndexerSearch.GameUpdateSearchService)
                        .GetMethod(nameof(IndexerSearch.GameUpdateSearchService.Handle), new[] { typeof(GameFileAddedEvent) })
                        .GetCustomAttributes(typeof(EventHandleOrderAttribute), true)
                        .Cast<EventHandleOrderAttribute>()
                        .SingleOrDefault();

            order.Should().NotBeNull();
            order.EventHandleOrder.Should().Be(EventHandleOrder.Last);
        }
    }
}
