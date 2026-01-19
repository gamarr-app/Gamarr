using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Games.AlternativeTitles
{
    public interface IAlternativeTitleRepository : IBasicRepository<AlternativeTitle>
    {
        List<AlternativeTitle> FindByGameMetadataId(int gameId);
        List<AlternativeTitle> FindByCleanTitles(List<string> cleanTitles);
        void DeleteForGames(List<int> gameIds);
    }

    public class AlternativeTitleRepository : BasicRepository<AlternativeTitle>, IAlternativeTitleRepository
    {
        public AlternativeTitleRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<AlternativeTitle> FindByGameMetadataId(int gameId)
        {
            return Query(x => x.GameMetadataId == gameId);
        }

        public List<AlternativeTitle> FindByCleanTitles(List<string> cleanTitles)
        {
            return Query(x => cleanTitles.Contains(x.CleanTitle));
        }

        public void DeleteForGames(List<int> gameIds)
        {
            Delete(x => gameIds.Contains(x.GameMetadataId));
        }
    }
}
