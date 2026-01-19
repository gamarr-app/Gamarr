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
        List<GameMetadata> FindById(List<int> igdbIds);
        List<GameMetadata> FindBySteamAppIds(List<int> steamAppIds);
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

            // Get existing metadata - use SteamAppId for Steam games, IgdbId for others
            var steamAppIds = metadatas.Where(x => x.SteamAppId > 0).Select(x => x.SteamAppId).ToList();
            var igdbIds = metadatas.Where(x => x.IgdbId > 0).Select(x => x.IgdbId).ToList();

            var existingBySteamId = steamAppIds.Any() ? FindBySteamAppIds(steamAppIds) : new List<GameMetadata>();
            var existingByIgdbId = igdbIds.Any() ? FindById(igdbIds) : new List<GameMetadata>();

            foreach (var metadata in metadatas)
            {
                GameMetadata existingMetadata = null;

                // Try to find by SteamAppId first (primary identifier)
                if (metadata.SteamAppId > 0)
                {
                    existingMetadata = existingBySteamId.SingleOrDefault(x => x.SteamAppId == metadata.SteamAppId);
                }

                // Fall back to IgdbId if no Steam match and IgdbId is set
                if (existingMetadata == null && metadata.IgdbId > 0)
                {
                    existingMetadata = existingByIgdbId.SingleOrDefault(x => x.IgdbId == metadata.IgdbId);
                }

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

        public List<GameMetadata> FindBySteamAppIds(List<int> steamAppIds)
        {
            return Query(x => Enumerable.Contains(steamAppIds, x.SteamAppId));
        }
    }
}
