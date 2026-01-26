using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Validation.Paths
{
    public class RootFolderValidator
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderValidator(IRootFolderService rootFolderService)
        {
            _rootFolderService = rootFolderService;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return true;
            }

            return !_rootFolderService.All().Exists(r => r.Path.IsPathValid(PathValidationType.CurrentOs) && r.Path.PathEquals(value));
        }
    }
}
