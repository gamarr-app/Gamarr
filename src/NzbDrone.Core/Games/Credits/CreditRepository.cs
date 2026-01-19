using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Games.Credits
{
    public interface ICreditRepository : IBasicRepository<Credit>
    {
        List<Credit> FindByGameMetadataId(int gameId);
        void DeleteForGames(List<int> gameIds);
    }

    public class CreditRepository : BasicRepository<Credit>, ICreditRepository
    {
        public CreditRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Credit> FindByGameMetadataId(int gameId)
        {
            return Query(x => x.GameMetadataId == gameId);
        }

        public void DeleteForGames(List<int> gameIds)
        {
            Delete(x => gameIds.Contains(x.GameMetadataId));
        }
    }
}
