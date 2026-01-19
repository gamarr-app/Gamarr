using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource.SkyHook.Resource;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Config
{
    public class MetadataConfigResource : RestResource
    {
        public TMDbCountryCode CertificationCountry { get; set; }
    }

    public static class MetadataConfigResourceMapper
    {
        public static MetadataConfigResource ToResource(IConfigService model)
        {
            return new MetadataConfigResource
            {
                CertificationCountry = model.CertificationCountry,
            };
        }
    }
}
