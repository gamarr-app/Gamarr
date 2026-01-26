using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Validation.Paths
{
    public class RootFolderExistsValidator
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderExistsValidator(IRootFolderService rootFolderService)
        {
            _rootFolderService = rootFolderService;
        }

        public bool Validate(string value)
        {
            return value == null || _rootFolderService.All().Exists(r => r.Path.IsPathValid(PathValidationType.CurrentOs) && r.Path.PathEquals(value));
        }
    }
}
