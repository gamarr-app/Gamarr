using System.Linq;
using System.Runtime.InteropServices;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation.Sentry;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
using Sentry;

namespace NzbDrone.Core.Instrumentation
{
    public class ReconfigureSentry : IHandleAsync<ApplicationStartedEvent>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IPlatformInfo _platformInfo;
        private readonly IMainDatabase _database;
        private readonly IAnalyticsService _analyticsService;
        private readonly IOsInfo _osInfo;

        public ReconfigureSentry(IConfigFileProvider configFileProvider,
                                 IPlatformInfo platformInfo,
                                 IMainDatabase database,
                                 IAnalyticsService analyticsService,
                                 IOsInfo osInfo)
        {
            _configFileProvider = configFileProvider;
            _platformInfo = platformInfo;
            _database = database;
            _analyticsService = analyticsService;
            _osInfo = osInfo;
        }

        public void Reconfigure()
        {
            // Extended sentry config
            var sentryTarget = LogManager.Configuration.AllTargets.OfType<SentryTarget>().FirstOrDefault();
            if (sentryTarget != null)
            {
                sentryTarget.UpdateScope(_database.Version, _database.Migration, _configFileProvider.Branch, _platformInfo);
            }
        }

        private void SendInstanceCheckIn()
        {
            if (!_analyticsService.IsEnabled)
            {
                return;
            }

            var sentryEvent = new SentryEvent
            {
                Message = new SentryMessage { Message = "instance.checkin" },
                Level = SentryLevel.Info,
                Logger = "Telemetry"
            };

            sentryEvent.SetTag("checkin", "true");
            sentryEvent.SetTag("app_version", BuildInfo.Version.ToString());
            sentryEvent.SetTag("branch", _configFileProvider.Branch);
            sentryEvent.SetTag("os", OsInfo.Os.ToString().ToLowerInvariant());
            sentryEvent.SetTag("os_name", _osInfo.FullName);
            sentryEvent.SetTag("arch", RuntimeInformation.OSArchitecture.ToString());
            sentryEvent.SetTag("runtime", $"{PlatformInfo.PlatformName} {_platformInfo.Version}");
            sentryEvent.SetTag("is_docker", _osInfo.IsDocker.ToString());
            sentryEvent.SetTag("is_production", RuntimeInfo.IsProduction.ToString());

            SentrySdk.CaptureEvent(sentryEvent);
        }

        public void HandleAsync(ApplicationStartedEvent message)
        {
            Reconfigure();
            SendInstanceCheckIn();
        }
    }
}
