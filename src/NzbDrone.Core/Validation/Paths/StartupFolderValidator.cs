using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Validation.Paths
{
    public class StartupFolderValidator
    {
        private readonly IAppFolderInfo _appFolderInfo;

        public StartupFolderValidator(IAppFolderInfo appFolderInfo)
        {
            _appFolderInfo = appFolderInfo;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return true;
            }

            var startupFolder = _appFolderInfo.StartUpFolder;

            if (startupFolder.PathEquals(value))
            {
                return false;
            }

            if (startupFolder.IsParentPath(value))
            {
                return false;
            }

            return true;
        }
    }
}
