using NzbDrone.Common.Disk;

namespace NzbDrone.Core.Validation
{
    public class FolderChmodValidator
    {
        private readonly IDiskProvider _diskProvider;

        public FolderChmodValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return false;
            }

            return _diskProvider.IsValidFolderPermissionMask(value);
        }
    }
}
