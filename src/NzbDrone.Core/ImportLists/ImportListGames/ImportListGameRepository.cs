using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.ImportLists.ImportListGames
{
    public interface IImportListGameRepository : IBasicRepository<ImportListGame>
    {
        List<ImportListGame> GetAllForLists(List<int> listIds);
        bool ExistsByMetadataId(int metadataId);
    }

    public class ImportListGameRepository : BasicRepository<ImportListGame>, IImportListGameRepository
    {
        public ImportListGameRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<ImportListGame> GetAllForLists(List<int> listIds)
        {
            return Query(x => listIds.Contains(x.ListId));
        }

        public bool ExistsByMetadataId(int metadataId)
        {
            return Query(x => x.GameMetadataId == metadataId).Any();
        }
    }
}
