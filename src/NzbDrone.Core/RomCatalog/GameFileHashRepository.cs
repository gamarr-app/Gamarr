using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface IGameFileHashRepository : IBasicRepository<GameFileHash>
    {
        List<GameFileHash> GetByGameFileIds(List<int> gameFileIds);
        void DeleteByGameFileIds(List<int> gameFileIds);
    }

    public class GameFileHashRepository : BasicRepository<GameFileHash>, IGameFileHashRepository
    {
        public GameFileHashRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<GameFileHash> GetByGameFileIds(List<int> gameFileIds)
        {
            return gameFileIds.Count == 0 ? new List<GameFileHash>() : Query(x => gameFileIds.Contains(x.GameFileId));
        }

        public void DeleteByGameFileIds(List<int> gameFileIds)
        {
            if (gameFileIds.Count == 0)
            {
                return;
            }

            Delete(x => gameFileIds.Contains(x.GameFileId));
        }
    }
}
