using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Validation
{
    public class QualityProfileNameInUseValidator
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileNameInUseValidator(IQualityProfileService qualityProfileService)
        {
            _qualityProfileService = qualityProfileService;
        }

        public bool Validate(int id, string name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return true;
            }

            return !_qualityProfileService.All().Any(p => p.Name.EqualsIgnoreCase(name) && p.Id != id);
        }
    }
}
