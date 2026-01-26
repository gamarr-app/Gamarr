using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Validation.Paths
{
    public class RecycleBinValidator
    {
        private readonly IConfigService _configService;

        public RecycleBinValidator(IConfigService configService)
        {
            _configService = configService;
        }

        public bool Validate(string value)
        {
            var recycleBin = _configService.RecycleBin;

            if (value == null || recycleBin.IsNullOrWhiteSpace())
            {
                return true;
            }

            if (recycleBin.PathEquals(value))
            {
                return false;
            }

            if (recycleBin.IsParentPath(value))
            {
                return false;
            }

            return true;
        }
    }
}
