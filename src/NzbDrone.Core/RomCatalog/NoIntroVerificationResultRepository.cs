using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroVerificationResultRepository : IBasicRepository<NoIntroVerificationResult>
    {
    }

    public class NoIntroVerificationResultRepository : BasicRepository<NoIntroVerificationResult>, INoIntroVerificationResultRepository
    {
        public NoIntroVerificationResultRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
