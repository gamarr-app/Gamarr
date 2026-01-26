using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Validation.Paths
{
    public class RootFolderAncestorValidator
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderAncestorValidator(IRootFolderService rootFolderService)
        {
            _rootFolderService = rootFolderService;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return true;
            }

            return !_rootFolderService.All().Any(s => value.IsParentPath(s.Path));
        }
    }
}
