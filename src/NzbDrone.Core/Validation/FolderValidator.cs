using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Validation
{
    public class FolderValidator
    {
        public bool Validate(string value)
        {
            if (value == null)
            {
                return false;
            }

            return value.IsPathValid(PathValidationType.CurrentOs);
        }
    }
}
