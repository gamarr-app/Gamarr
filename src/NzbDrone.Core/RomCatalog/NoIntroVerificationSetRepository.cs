using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroVerificationSetRepository : IBasicRepository<NoIntroVerificationSet>
    {
    }

    public class NoIntroVerificationSetRepository : BasicRepository<NoIntroVerificationSet>, INoIntroVerificationSetRepository
    {
        public NoIntroVerificationSetRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
