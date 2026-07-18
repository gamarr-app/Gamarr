using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogSourceRepository : IBasicRepository<NoIntroCatalogSource>
    {
    }

    public class NoIntroCatalogSourceRepository : BasicRepository<NoIntroCatalogSource>, INoIntroCatalogSourceRepository
    {
        public NoIntroCatalogSourceRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
