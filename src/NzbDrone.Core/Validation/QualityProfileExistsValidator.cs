using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Validation
{
    public class QualityProfileExistsValidator
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileExistsValidator(IQualityProfileService qualityProfileService)
        {
            _qualityProfileService = qualityProfileService;
        }

        public bool Validate(int qualityProfileId)
        {
            if (qualityProfileId == 0)
            {
                return true;
            }

            return _qualityProfileService.Exists(qualityProfileId);
        }
    }
}
