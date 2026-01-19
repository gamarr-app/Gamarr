using System;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class SystemTimeCheck : HealthCheckBase
    {
        private readonly IHttpClient _client;
        private readonly IHttpRequestBuilderFactory _cloudRequestBuilder;
        private readonly Logger _logger;

        public SystemTimeCheck(IHttpClient client, IGamarrCloudRequestBuilder cloudRequestBuilder, Logger logger, ILocalizationService localizationService)
            : base(localizationService)
        {
            _client = client;
            _cloudRequestBuilder = cloudRequestBuilder.Services;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            // Disabled: gamarr.servarr.com doesn't exist for this fork
            return new HealthCheck(GetType());
        }
    }

    public class ServiceTimeResponse
    {
        public DateTime DateTimeUtc { get; set; }
    }
}
