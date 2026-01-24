using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games.Collections
{
    [TestFixture]
    public class GameCollectionServiceFixture : CoreTest<GameCollectionService>
    {
        private GameCollection _collection;
        private GameCollection _collection2;

        [SetUp]
        public void Setup()
        {
            _collection = new GameCollection
            {
                Id = 1,
                IgdbId = 100,
                Title = "Test Collection"
            };

            _collection2 = new GameCollection
            {
                Id = 2,
                IgdbId = 200,
                Title = "Second Collection"
            };
        }

        [Test]
        public void should_return_collection_by_id()
        {
            Mocker.GetMock<IGameCollectionRepository>()
                  .Setup(s => s.Get(1))
                  .Returns(_collection);

            var result = Subject.GetCollection(1);

            result.Should().BeSameAs(_collection);
        }

        [Test]
        public void should_return_all_collections()
        {
            var collections = new List<GameCollection> { _collection, _collection2 };

            Mocker.GetMock<IGameCollectionRepository>()
                  .Setup(s => s.All())
                  .Returns(collections);

            var result = Subject.GetAllCollections();

            result.Should().HaveCount(2);
            result.Should().Contain(_collection);
            result.Should().Contain(_collection2);
        }

        [Test]
        public void should_add_collection_and_publish_event()
        {
            Mocker.GetMock<IGameCollectionRepository>()
                  .Setup(s => s.GetByIgdbId(_collection.IgdbId))
                  .Returns((GameCollection)null);

            Mocker.GetMock<IGameCollectionRepository>()
                  .Setup(s => s.Insert(_collection))
                  .Returns(_collection);

            var result = Subject.AddCollection(_collection);

            result.Should().BeSameAs(_collection);

            Mocker.GetMock<IGameCollectionRepository>()
                  .Verify(v => v.Insert(_collection), Times.Once());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.Is<CollectionAddedEvent>(e => e.Collection == _collection)), Times.Once());
        }

        [Test]
        public void should_return_existing_when_adding_duplicate_igdb_id()
        {
            Mocker.GetMock<IGameCollectionRepository>()
                  .Setup(s => s.GetByIgdbId(_collection.IgdbId))
                  .Returns(_collection);

            var newCollection = new GameCollection
            {
                IgdbId = _collection.IgdbId,
                Title = "Duplicate"
            };

            var result = Subject.AddCollection(newCollection);

            result.Should().BeSameAs(_collection);

            Mocker.GetMock<IGameCollectionRepository>()
                  .Verify(v => v.Insert(It.IsAny<GameCollection>()), Times.Never());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<CollectionAddedEvent>()), Times.Never());
        }

        [Test]
        public void should_update_collection_and_publish_event()
        {
            var updatedCollection = new GameCollection
            {
                Id = 1,
                IgdbId = 100,
                Title = "Updated Title"
            };

            Mocker.GetMock<IGameCollectionRepository>()
                  .Setup(s => s.Get(1))
                  .Returns(_collection);

            Mocker.GetMock<IGameCollectionRepository>()
                  .Setup(s => s.Update(updatedCollection))
                  .Returns(updatedCollection);

            var result = Subject.UpdateCollection(updatedCollection);

            result.Should().BeSameAs(updatedCollection);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.Is<CollectionEditedEvent>(e =>
                      e.Collection == updatedCollection && e.OldCollection == _collection)), Times.Once());
        }

        [Test]
        public void should_delete_collection_and_publish_event()
        {
            Subject.RemoveCollection(_collection);

            Mocker.GetMock<IGameCollectionRepository>()
                  .Verify(v => v.Delete(_collection), Times.Once());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.Is<CollectionDeletedEvent>(e => e.Collection == _collection)), Times.Once());
        }

        [Test]
        public void should_delete_orphaned_collection_when_no_games_reference_it()
        {
            var metadata = new GameMetadata
            {
                CollectionIgdbId = _collection.IgdbId
            };

            var game = new Game
            {
                Id = 10,
                GameMetadata = new LazyLoaded<GameMetadata>(metadata)
            };

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(_collection.IgdbId))
                  .Returns(new List<Game>());

            Mocker.GetMock<IGameCollectionRepository>()
                  .Setup(s => s.GetByIgdbId(_collection.IgdbId))
                  .Returns(_collection);

            var message = new GamesDeletedEvent(new List<Game> { game }, false, false);

            Subject.HandleAsync(message);

            Mocker.GetMock<IGameCollectionRepository>()
                  .Verify(v => v.Delete(_collection.Id), Times.Once());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.Is<CollectionDeletedEvent>(e => e.Collection == _collection)), Times.Once());
        }

        [Test]
        public void should_keep_collection_when_other_games_still_reference_it()
        {
            var metadata = new GameMetadata
            {
                CollectionIgdbId = _collection.IgdbId
            };

            var deletedGame = new Game
            {
                Id = 10,
                GameMetadata = new LazyLoaded<GameMetadata>(metadata)
            };

            var remainingGame = new Game
            {
                Id = 20,
                GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata { CollectionIgdbId = _collection.IgdbId })
            };

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(_collection.IgdbId))
                  .Returns(new List<Game> { remainingGame });

            var message = new GamesDeletedEvent(new List<Game> { deletedGame }, false, false);

            Subject.HandleAsync(message);

            Mocker.GetMock<IGameCollectionRepository>()
                  .Verify(v => v.Delete(It.IsAny<int>()), Times.Never());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<CollectionDeletedEvent>()), Times.Never());
        }

        [Test]
        public void should_handle_game_with_no_collection_gracefully()
        {
            var metadata = new GameMetadata
            {
                CollectionIgdbId = 0
            };

            var game = new Game
            {
                Id = 10,
                GameMetadata = new LazyLoaded<GameMetadata>(metadata)
            };

            var message = new GamesDeletedEvent(new List<Game> { game }, false, false);

            Subject.HandleAsync(message);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.GetGamesByCollectionIgdbId(It.IsAny<int>()), Times.Never());

            Mocker.GetMock<IGameCollectionRepository>()
                  .Verify(v => v.Delete(It.IsAny<int>()), Times.Never());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<CollectionDeletedEvent>()), Times.Never());
        }
    }
}
