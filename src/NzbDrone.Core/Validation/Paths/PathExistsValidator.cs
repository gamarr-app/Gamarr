using NzbDrone.Common.Disk;

namespace NzbDrone.Core.Validation.Paths
{
    public class PathExistsValidator
    {
        private readonly IDiskProvider _diskProvider;

        public PathExistsValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return false;
            }

            return _diskProvider.FolderExists(value);
        }
    }
}
