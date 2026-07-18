using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroVerificationSnapshotRepository : IBasicRepository<NoIntroVerificationSnapshot>
    {
    }

    public class NoIntroVerificationSnapshotRepository : BasicRepository<NoIntroVerificationSnapshot>, INoIntroVerificationSnapshotRepository
    {
        public NoIntroVerificationSnapshotRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
