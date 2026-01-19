using NzbDrone.Core.Configuration;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Config
{
    public class MetadataConfigResource : RestResource
    {
        public CountryCode CertificationCountry { get; set; }
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
