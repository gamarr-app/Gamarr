using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RootFolders
{
    public interface IPlatformRootFolderRepository : IBasicRepository<PlatformRootFolder>
    {
        PlatformRootFolder FindByPlatform(PlatformFamily platform);
    }

    public class PlatformRootFolderRepository : BasicRepository<PlatformRootFolder>, IPlatformRootFolderRepository
    {
        public PlatformRootFolderRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override bool PublishModelEvents => true;

        public PlatformRootFolder FindByPlatform(PlatformFamily platform)
        {
            return Query(x => x.Platform == platform).FirstOrDefault();
        }
    }
}
