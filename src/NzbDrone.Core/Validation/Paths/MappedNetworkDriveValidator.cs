using System.IO;
using System.Text.RegularExpressions;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Core.Validation.Paths
{
    public class MappedNetworkDriveValidator
    {
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly IDiskProvider _diskProvider;

        private static readonly Regex DriveRegex = new Regex(@"[a-z]\:\\", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public MappedNetworkDriveValidator(IRuntimeInfo runtimeInfo, IDiskProvider diskProvider)
        {
            _runtimeInfo = runtimeInfo;
            _diskProvider = diskProvider;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return false;
            }

            if (OsInfo.IsNotWindows)
            {
                return true;
            }

            if (!_runtimeInfo.IsWindowsService)
            {
                return true;
            }

            if (!DriveRegex.IsMatch(value))
            {
                return true;
            }

            var mount = _diskProvider.GetMount(value);

            return mount is not { DriveType: DriveType.Network };
        }
    }
}
