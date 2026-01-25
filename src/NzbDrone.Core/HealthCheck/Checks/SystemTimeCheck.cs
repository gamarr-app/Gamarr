using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class SystemTimeCheck : HealthCheckBase
    {
        private const string WorldTimeApiUrl = "https://worldtimeapi.org/api/timezone/Etc/UTC";

        private readonly IHttpClient _client;
        private readonly Logger _logger;

        public SystemTimeCheck(IHttpClient client, Logger logger, ILocalizationService localizationService)
            : base(localizationService)
        {
            _client = client;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            try
            {
                var request = new HttpRequest(WorldTimeApiUrl);
                var response = _client.Execute(request);
                var result = Json.Deserialize<WorldTimeApiResponse>(response.Content);
                var systemTime = DateTime.UtcNow;

                if (Math.Abs((result.Datetime - systemTime).TotalDays) >= 1)
                {
                    _logger.Error("System time mismatch. SystemTime: {0} ApiTime: {1}", systemTime, result.Datetime);
                    return new HealthCheck(GetType(), HealthCheckResult.Error, _localizationService.GetLocalizedString("SystemTimeCheckMessage"), "#system-time-off");
                }
            }
            catch (Exception ex)
            {
                // Time check is not critical - if the API is unavailable, just skip the check
                _logger.Debug(ex, "Unable to check system time against external API");
            }

            return new HealthCheck(GetType());
        }
    }

    public class WorldTimeApiResponse
    {
        public DateTime Datetime { get; set; }
    }
}
