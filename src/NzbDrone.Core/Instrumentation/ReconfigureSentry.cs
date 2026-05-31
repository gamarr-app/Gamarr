using System.Collections.Generic;
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

            // Record install context as a breadcrumb rather than a captured event.
            // Capturing it as an event made every startup show up as a Sentry issue
            // (logger=Telemetry, level=info), which polluted the issues view. As a
            // breadcrumb the data still rides along with any real error event from
            // this instance, but no synthetic issue is created on startup.
            SentrySdk.AddBreadcrumb(
                "instance.checkin",
                category: "Telemetry",
                level: BreadcrumbLevel.Info,
                data: new Dictionary<string, string>
                {
                    { "app_version", BuildInfo.Version.ToString() },
                    { "branch", _configFileProvider.Branch },
                    { "os", OsInfo.Os.ToString().ToLowerInvariant() },
                    { "os_name", _osInfo.FullName },
                    { "arch", RuntimeInformation.OSArchitecture.ToString() },
                    { "runtime", $"{PlatformInfo.PlatformName} {_platformInfo.Version}" },
                    { "is_docker", _osInfo.IsDocker.ToString() },
                    { "is_production", RuntimeInfo.IsProduction.ToString() },
                });
        }

        public void HandleAsync(ApplicationStartedEvent message)
        {
            Reconfigure();
            SendInstanceCheckIn();
        }
    }
}
