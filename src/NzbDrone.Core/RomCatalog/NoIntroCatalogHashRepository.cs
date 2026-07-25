using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogHashRepository : IBasicRepository<NoIntroCatalogHash>
    {
        List<NoIntroCatalogHash> GetByEntryIds(List<int> entryIds);
        void DeleteByEntryIds(List<int> entryIds);
    }

    public class NoIntroCatalogHashRepository : BasicRepository<NoIntroCatalogHash>, INoIntroCatalogHashRepository
    {
        public NoIntroCatalogHashRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<NoIntroCatalogHash> GetByEntryIds(List<int> entryIds)
        {
            return entryIds.Count == 0 ? new List<NoIntroCatalogHash>() : Query(x => entryIds.Contains(x.CatalogEntryId));
        }

        public void DeleteByEntryIds(List<int> entryIds)
        {
            if (entryIds.Count == 0)
            {
                return;
            }

            Delete(x => entryIds.Contains(x.CatalogEntryId));
        }
    }
}
