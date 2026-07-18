using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Games.Components
{
    public interface IGameComponentRepository : IBasicRepository<GameComponent>
    {
        List<GameComponent> GetByGame(int gameId);
        List<GameComponent> GetMonitoredDlc();
        GameComponent Find(int gameId, GameComponentType type, string key);
        void DeleteByGame(int gameId);
    }

    public class GameComponentRepository : BasicRepository<GameComponent>, IGameComponentRepository
    {
        public GameComponentRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<GameComponent> GetByGame(int gameId)
        {
            return Query(c => c.GameId == gameId);
        }

        public List<GameComponent> GetMonitoredDlc()
        {
            return Query(c => c.Monitored == true && c.ComponentType == GameComponentType.Dlc);
        }

        public GameComponent Find(int gameId, GameComponentType type, string key)
        {
            return Query(c => c.GameId == gameId && c.ComponentType == type && c.Key == key).FirstOrDefault();
        }

        public void DeleteByGame(int gameId)
        {
            Delete(c => c.GameId == gameId);
        }
    }
}
