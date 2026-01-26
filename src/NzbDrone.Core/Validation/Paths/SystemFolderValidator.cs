using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Validation.Paths
{
    public class SystemFolderValidator
    {
        public bool Validate(string value)
        {
            if (value == null)
            {
                return true;
            }

            foreach (var systemFolder in SystemFolders.GetSystemFolders())
            {
                if (systemFolder.PathEquals(value))
                {
                    return false;
                }

                if (systemFolder.IsParentPath(value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
