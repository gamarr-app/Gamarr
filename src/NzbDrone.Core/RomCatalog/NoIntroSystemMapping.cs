using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroSystemMapping : ModelBase
    {
        public string SystemKey { get; set; }
        public string DisplayName { get; set; }
        public PlatformFamily PlatformFamily { get; set; }
        public string RootRelativePathPattern { get; set; }
    }
}
