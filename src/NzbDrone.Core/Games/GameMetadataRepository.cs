using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Games
{
    public interface IGameMetadataRepository : IBasicRepository<GameMetadata>
    {
        GameMetadata FindByIgdbId(int igdbId);
        GameMetadata FindByImdbId(string imdbId);
        List<GameMetadata> FindById(List<int> igdbIds);
        List<GameMetadata> GetGamesWithCollections();
        List<GameMetadata> GetGamesByCollectionIgdbId(int collectionId);
        bool UpsertMany(List<GameMetadata> metadatas);
    }

    public class GameMetadataRepository : BasicRepository<GameMetadata>, IGameMetadataRepository
    {
        private readonly Logger _logger;

        public GameMetadataRepository(IMainDatabase database, IEventAggregator eventAggregator, Logger logger)
            : base(database, eventAggregator)
        {
            _logger = logger;
        }

        public GameMetadata FindByIgdbId(int igdbId)
        {
            return Query(x => x.IgdbId == igdbId).FirstOrDefault();
        }

        public GameMetadata FindByImdbId(string imdbId)
        {
            return Query(x => x.ImdbId == imdbId).FirstOrDefault();
        }

        public List<GameMetadata> FindById(List<int> igdbIds)
        {
            return Query(x => Enumerable.Contains(igdbIds, x.IgdbId));
        }

        public List<GameMetadata> GetGamesWithCollections()
        {
            return Query(x => x.CollectionIgdbId > 0);
        }

        public List<GameMetadata> GetGamesByCollectionIgdbId(int collectionId)
        {
            return Query(x => x.CollectionIgdbId == collectionId);
        }

        public bool UpsertMany(List<GameMetadata> metadatas)
        {
            var updateList = new List<GameMetadata>();
            var addList = new List<GameMetadata>();
            var upToDateCount = 0;

            var existingMetadatas = FindById(metadatas.Select(x => x.IgdbId).ToList());

            foreach (var metadata in metadatas)
            {
                var existingMetadata = existingMetadatas.SingleOrDefault(x => x.IgdbId == metadata.IgdbId);

                if (existingMetadata != null)
                {
                    metadata.UseDbFieldsFrom(existingMetadata);

                    if (!metadata.Equals(existingMetadata))
                    {
                        updateList.Add(metadata);
                    }
                    else
                    {
                        upToDateCount++;
                    }
                }
                else
                {
                    addList.Add(metadata);
                }
            }

            UpdateMany(updateList);
            InsertMany(addList);

            _logger.Debug("{0} game metadata up to date; Updating {1}, Adding {2} entries.", upToDateCount, updateList.Count, addList.Count);

            return updateList.Count > 0 || addList.Count > 0;
        }
    }
}
