using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Games.Collections
{
    public interface IGameCollectionRepository : IBasicRepository<GameCollection>
    {
        public GameCollection GetByIgdbId(int igdbId);
        bool UpsertMany(List<GameCollection> data);
    }

    public class GameCollectionRepository : BasicRepository<GameCollection>, IGameCollectionRepository
    {
        public GameCollectionRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public GameCollection GetByIgdbId(int igdbId)
        {
            return Query(x => x.IgdbId == igdbId).FirstOrDefault();
        }

        public List<GameCollection> GetByIgdbId(List<int> igdbIds)
        {
            return Query(x => Enumerable.Contains(igdbIds, x.IgdbId));
        }

        public bool UpsertMany(List<GameCollection> data)
        {
            var existingMetadata = GetByIgdbId(data.Select(x => x.IgdbId).ToList());
            var updateCollectionList = new List<GameCollection>();
            var addCollectionList = new List<GameCollection>();

            foreach (var collection in data)
            {
                var existing = existingMetadata.SingleOrDefault(x => x.IgdbId == collection.IgdbId);
                if (existing != null)
                {
                    // populate Id in remote data
                    collection.Id = existing.Id;

                    // responses vary, so try adding remote to what we have
                    if (!collection.Equals(existing))
                    {
                        updateCollectionList.Add(collection);
                    }
                    else
                    {
                    }
                }
                else
                {
                    addCollectionList.Add(collection);
                }
            }

            UpdateMany(updateCollectionList);
            InsertMany(addCollectionList);

            return updateCollectionList.Count > 0 || addCollectionList.Count > 0;
        }
    }
}
