using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Profiles.Delay
{
    public class DelayProfileTagInUseValidator
    {
        private readonly IDelayProfileService _delayProfileService;

        public DelayProfileTagInUseValidator(IDelayProfileService delayProfileService)
        {
            _delayProfileService = delayProfileService;
        }

        public bool Validate(HashSet<int> tags, int instanceId)
        {
            if (tags == null || tags.Empty())
            {
                return true;
            }

            return _delayProfileService.All().None(d => d.Id != instanceId && d.Tags.Intersect(tags).Any());
        }
    }
}
