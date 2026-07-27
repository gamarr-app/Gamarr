using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogEntryRepository : IBasicRepository<NoIntroCatalogEntry>
    {
        List<NoIntroCatalogEntry> GetBySourceId(int catalogSourceId);
        List<NoIntroCatalogEntry> GetByPlatformFamily(PlatformFamily platformFamily);
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

        public List<NoIntroCatalogEntry> GetByPlatformFamily(PlatformFamily platformFamily)
        {
            var relevantFamilies = NoIntroCatalogDefaults.GetRelevantPlatformFamilies(platformFamily).ToHashSet();
            return Query(x => relevantFamilies.Contains(x.PlatformFamily));
        }

        public void DeleteBySourceId(int catalogSourceId)
        {
            Delete(x => x.CatalogSourceId == catalogSourceId);
        }
    }
}
