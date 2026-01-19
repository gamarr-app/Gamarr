using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Games.Translations
{
    public interface IGameTranslationRepository : IBasicRepository<GameTranslation>
    {
        List<GameTranslation> FindByGameMetadataId(int gameMetadataId);
        List<GameTranslation> FindByLanguage(Language language);
        void DeleteForGames(List<int> gameIds);
    }

    public class GameTranslationRepository : BasicRepository<GameTranslation>, IGameTranslationRepository
    {
        public GameTranslationRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<GameTranslation> FindByGameMetadataId(int gameMetadataId)
        {
            return Query(x => x.GameMetadataId == gameMetadataId);
        }

        public List<GameTranslation> FindByLanguage(Language language)
        {
            return Query(x => x.Language == language);
        }

        public void DeleteForGames(List<int> gameIds)
        {
            Delete(x => gameIds.Contains(x.GameMetadataId));
        }
    }
}
