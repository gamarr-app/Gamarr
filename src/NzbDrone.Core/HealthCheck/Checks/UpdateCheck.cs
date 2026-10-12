using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Configuration.Events;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Update;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ConfigFileSavedEvent))]
    public class UpdateCheck : HealthCheckBase
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IUpdatePackageProvider _updatePackageProvider;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IDeploymentInfoProvider _deploymentInfoProvider;
        private readonly IOsInfo _osInfo;

        public UpdateCheck(IDiskProvider diskProvider,
                           IAppFolderInfo appFolderInfo,
                           IUpdatePackageProvider updatePackageProvider,
                           IConfigFileProvider configFileProvider,
                           IDeploymentInfoProvider deploymentInfoProvider,
                           IOsInfo osInfo,
                           ILocalizationService localizationService)
            : base(localizationService)
        {
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
            _updatePackageProvider = updatePackageProvider;
            _configFileProvider = configFileProvider;
            _deploymentInfoProvider = deploymentInfoProvider;
            _osInfo = osInfo;
        }

        public override HealthCheck Check()
        {
            var startupFolder = _appFolderInfo.StartUpFolder;
            var uiFolder = Path.Combine(startupFolder, "UI");

            if (_configFileProvider.UpdateAutomatically &&
                _configFileProvider.UpdateMechanism == UpdateMechanism.BuiltIn &&
                !_osInfo.IsDocker)
            {
                if (OsInfo.IsOsx && startupFolder.GetAncestorFolders().Contains("AppTranslocation"))
                {
                    return new HealthCheck(GetType(),
                        HealthCheckResult.Error,
                        HealthCheckReason.UpdateStartupTranslocation,
                        _localizationService.GetLocalizedString(
                            "UpdateCheckStartupTranslocationMessage",
                            new Dictionary<string, object>
                            {
                                { "startupFolder", startupFolder }
                            }),
                        "#cannot-install-update-because-startup-folder-is-in-an-app-translocation-folder.");
                }

                if (!_diskProvider.FolderWritable(startupFolder))
                {
                    return new HealthCheck(GetType(),
                        HealthCheckResult.Error,
                        HealthCheckReason.UpdateStartupNotWritable,
                        _localizationService.GetLocalizedString(
                            "UpdateCheckStartupNotWritableMessage",
                            new Dictionary<string, object>
                            {
                                { "startupFolder", startupFolder },
                                { "userName", Environment.UserName }
                            }),
                        "#cannot-install-update-because-startup-folder-is-not-writable-by-the-user");
                }

                if (!_diskProvider.FolderWritable(uiFolder))
                {
                    return new HealthCheck(GetType(),
                        HealthCheckResult.Error,
                        HealthCheckReason.UpdateUiNotWritable,
                        _localizationService.GetLocalizedString(
                            "UpdateCheckUINotWritableMessage",
                            new Dictionary<string, object>
                            {
                                { "uiFolder", uiFolder },
                                { "userName", Environment.UserName }
                            }),
                        "#cannot-install-update-because-ui-folder-is-not-writable-by-the-user");
                }
            }

            if (BuildInfo.BuildDateTime < DateTime.UtcNow.AddDays(-14))
            {
                // A single GitHub call covers both "is there an update" and
                // "how many releases behind" so a rate-limited or failed
                // call only costs one request, not two. GetRecentUpdates
                // already swallows HTTP exceptions and returns an empty list
                // on failure, so a GitHub outage/rate limit degrades quietly
                // here: no banner, no error noise.
                var newerReleases = _updatePackageProvider
                    .GetRecentUpdates(_configFileProvider.Branch, BuildInfo.Version)
                    .Where(u => u.Version != null && u.Version > BuildInfo.Version)
                    .OrderByDescending(u => u.Version)
                    .ToList();

                if (newerReleases.Any())
                {
                    var latestAvailable = newerReleases.First();
                    var versionsBehindText = newerReleases.Count >= 10 ? "10+" : newerReleases.Count.ToString();

                    var messageKey = _deploymentInfoProvider.PackageUpdateMechanism == UpdateMechanism.Docker
                        ? "UpdateAvailableDockerHealthCheckMessage"
                        : "UpdateAvailableHealthCheckMessage";

                    return new HealthCheck(GetType(),
                        HealthCheckResult.Warning,
                        HealthCheckReason.UpdateAvailable,
                        _localizationService.GetLocalizedString(messageKey, new Dictionary<string, object>
                        {
                            { "version", $"v{latestAvailable.Version}" },
                            { "currentVersion", $"v{BuildInfo.Version}" },
                            { "versionsBehind", versionsBehindText }
                        }),
                        "#new-update-is-available");
                }
            }

            return new HealthCheck(GetType());
        }
    }
}
