using NzbDrone.Common.Disk;

namespace NzbDrone.Core.Validation.Paths
{
    public class FolderWritableValidator
    {
        private readonly IDiskProvider _diskProvider;

        public FolderWritableValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        public bool Validate(string value)
        {
            if (value == null)
            {
                return false;
            }

            return _diskProvider.FolderWritable(value);
        }
    }
}
