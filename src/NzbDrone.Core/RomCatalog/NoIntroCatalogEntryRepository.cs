using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogEntryRepository : IBasicRepository<NoIntroCatalogEntry>
    {
        List<NoIntroCatalogEntry> GetBySourceId(int catalogSourceId);
        void DeleteBySourceId(int catalogSourceId);
    }

    public class NoIntroCatalogEntryRepository : BasicRepository<NoIntroCatalogEntry>, INoIntroCatalogEntryRepository
    {
        public NoIntroCatalogEntryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<NoIntroCatalogEntry> GetBySourceId(int catalogSourceId)
        {
            return Query(x => x.CatalogSourceId == catalogSourceId);
        }

        public void DeleteBySourceId(int catalogSourceId)
        {
            Delete(x => x.CatalogSourceId == catalogSourceId);
        }
    }
}
