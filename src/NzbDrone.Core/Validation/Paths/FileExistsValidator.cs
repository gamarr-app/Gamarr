using NzbDrone.Common.Disk;

namespace NzbDrone.Core.Validation.Paths
{
    public class FileExistsValidator
    {
        private readonly IDiskProvider _diskProvider;

        public FileExistsValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return false;
            }

            return _diskProvider.FileExists(value);
        }
    }
}
