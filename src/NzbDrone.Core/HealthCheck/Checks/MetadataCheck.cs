using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderUpdatedEvent<IMetadata>))]
    public class MetadataCheck : HealthCheckBase
    {
        public MetadataCheck(IMetadataFactory metadataFactory, ILocalizationService localizationService)
            : base(localizationService)
        {
        }

        public override HealthCheck Check()
        {
            // All deprecated metadata consumers have been removed
            return new HealthCheck(GetType());
        }
    }
}
