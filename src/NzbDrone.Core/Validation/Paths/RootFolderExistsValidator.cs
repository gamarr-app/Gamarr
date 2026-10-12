using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Validation.Paths
{
    // "Root folder" here means anywhere an add is allowed to land: a
    // configured RootFolder, or a per-platform mapping from
    // IPlatformRootFolderService. The latter is deliberately not required to
    // be a subpath of a RootFolder (PlatformRootFolderService.Validate only
    // checks that it's rooted), so a mapped path like /games/Switch is a
    // legitimate RootFolderPath even though it never appears in
    // IRootFolderService.All(). Before this checked only the latter, so any
    // add resolved through a platform mapping failed with "Root folder does
    // not exist" even though the path was real and on disk (#186).
    public class RootFolderExistsValidator
    {
        private readonly IRootFolderService _rootFolderService;
        private readonly IPlatformRootFolderService _platformRootFolderService;

        public RootFolderExistsValidator(IRootFolderService rootFolderService, IPlatformRootFolderService platformRootFolderService)
        {
            _rootFolderService = rootFolderService;
            _platformRootFolderService = platformRootFolderService;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return true;
            }

            if (_rootFolderService.All().Exists(r => r.Path.IsPathValid(PathValidationType.CurrentOs) && r.Path.PathEquals(value)))
            {
                return true;
            }

            return _platformRootFolderService.All().Exists(p => p.Path.IsNotNullOrWhiteSpace() && p.Path.IsPathValid(PathValidationType.CurrentOs) && p.Path.PathEquals(value));
        }
    }
}
