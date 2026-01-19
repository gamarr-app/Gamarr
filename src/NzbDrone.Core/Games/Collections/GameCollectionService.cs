using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.Games.Collections
{
    public interface IGameCollectionService
    {
        GameCollection AddCollection(GameCollection collection);
        GameCollection GetCollection(int id);
        GameCollection FindByIgdbId(int igdbId);
        IEnumerable<GameCollection> GetCollections(IEnumerable<int> ids);
        List<GameCollection> GetAllCollections();
        GameCollection UpdateCollection(GameCollection collection);
        List<GameCollection> UpdateCollections(List<GameCollection> collections);
        void RemoveCollection(GameCollection collection);
        bool Upsert(GameCollection collection);
        bool UpsertMany(List<GameCollection> collections);
    }

    public class GameCollectionService : IGameCollectionService, IHandleAsync<GamesDeletedEvent>
    {
        private readonly IGameCollectionRepository _repo;
        private readonly IGameService _gameService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public GameCollectionService(IGameCollectionRepository repo, IGameService gameService, IEventAggregator eventAggregator, Logger logger)
        {
            _repo = repo;
            _gameService = gameService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public GameCollection AddCollection(GameCollection newCollection)
        {
            var existing = _repo.GetByIgdbId(newCollection.IgdbId);

            if (existing == null)
            {
                var collection = _repo.Insert(newCollection);

                _eventAggregator.PublishEvent(new CollectionAddedEvent(collection));

                return collection;
            }

            return existing;
        }

        public GameCollection GetCollection(int id)
        {
            return _repo.Get(id);
        }

        public IEnumerable<GameCollection> GetCollections(IEnumerable<int> ids)
        {
            return _repo.Get(ids);
        }

        public List<GameCollection> GetAllCollections()
        {
            return _repo.All().ToList();
        }

        public GameCollection UpdateCollection(GameCollection collection)
        {
            var storedCollection = GetCollection(collection.Id);

            var updatedCollection =  _repo.Update(collection);

            _eventAggregator.PublishEvent(new CollectionEditedEvent(updatedCollection, storedCollection));

            return updatedCollection;
        }

        public List<GameCollection> UpdateCollections(List<GameCollection> collections)
        {
            _logger.Debug("Updating {0} game collections", collections.Count);

            foreach (var c in collections)
            {
                _logger.Trace("Updating: {0}", c.Title);
            }

            _repo.UpdateMany(collections);
            _logger.Debug("{0} game collections updated", collections.Count);

            return collections;
        }

        public void RemoveCollection(GameCollection collection)
        {
            _repo.Delete(collection);

            _eventAggregator.PublishEvent(new CollectionDeletedEvent(collection));
        }

        public bool Upsert(GameCollection collection)
        {
            return _repo.UpsertMany(new List<GameCollection> { collection });
        }

        public bool UpsertMany(List<GameCollection> collections)
        {
            return _repo.UpsertMany(collections);
        }

        public void HandleAsync(GamesDeletedEvent message)
        {
            var collections = message.Games.Select(x => x.GameMetadata.Value.CollectionIgdbId).Distinct();

            foreach (var collectionIgdbId in collections)
            {
                if (collectionIgdbId == 0 || _gameService.GetGamesByCollectionIgdbId(collectionIgdbId).Any())
                {
                    continue;
                }

                var collection = FindByIgdbId(collectionIgdbId);

                if (collection == null)
                {
                    continue;
                }

                _repo.Delete(collection.Id);

                _eventAggregator.PublishEvent(new CollectionDeletedEvent(collection));
            }
        }

        public GameCollection FindByIgdbId(int igdbId)
        {
            return _repo.GetByIgdbId(igdbId);
        }
    }
}
