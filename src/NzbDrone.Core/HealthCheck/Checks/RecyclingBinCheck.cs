using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(GameFileImportedEvent), CheckOnCondition.FailedOnly)]
    [CheckOn(typeof(GameImportFailedEvent), CheckOnCondition.SuccessfulOnly)]
    public class RecyclingBinCheck : HealthCheckBase
    {
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderService _rootFolderService;

        public RecyclingBinCheck(IConfigService configService, IDiskProvider diskProvider, IRootFolderService rootFolderService, ILocalizationService localizationService)
            : base(localizationService)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _rootFolderService = rootFolderService;
        }

        public override HealthCheck Check()
        {
            var recycleBin = _configService.RecycleBin;

            if (recycleBin.IsNotNullOrWhiteSpace())
            {
                if (!_diskProvider.FolderWritable(recycleBin))
                {
                    return new HealthCheck(GetType(),
                        HealthCheckResult.Error,
                        HealthCheckReason.RecycleBinUnableToWrite,
                        _localizationService.GetLocalizedString("RecycleBinUnableToWriteHealthCheck", new Dictionary<string, object>
                        {
                            { "path", recycleBin }
                        }),
                        "#cannot-write-recycle-bin");
                }

                return new HealthCheck(GetType());
            }

            // With no configured recycling bin, RecycleBinProvider falls back to a '.recycle' folder
            // inside each root folder, so that fallback is what has to be writable.
            if (!_configService.RecycleBinEnabled)
            {
                return new HealthCheck(GetType());
            }

            var unwritablePaths = new List<string>();

            foreach (var rootFolder in _rootFolderService.All())
            {
                // A missing root folder is RootFolderCheck's job to report.
                if (!_diskProvider.FolderExists(rootFolder.Path))
                {
                    continue;
                }

                var fallbackPath = Path.Combine(rootFolder.Path, ".recycle");

                if (_diskProvider.FolderExists(fallbackPath))
                {
                    if (!_diskProvider.FolderWritable(fallbackPath))
                    {
                        unwritablePaths.Add(fallbackPath);
                    }
                }
                else if (!_diskProvider.FolderWritable(rootFolder.Path))
                {
                    // The provider would have to create '.recycle' here and cannot.
                    unwritablePaths.Add(rootFolder.Path);
                }
            }

            if (unwritablePaths.Any())
            {
                return new HealthCheck(GetType(),
                    HealthCheckResult.Error,
                    HealthCheckReason.RecycleBinUnableToWrite,
                    _localizationService.GetLocalizedString("RecycleBinRootFolderUnableToWriteHealthCheck", new Dictionary<string, object>
                    {
                        { "path", string.Join(" | ", unwritablePaths) }
                    }),
                    "#cannot-write-recycle-bin");
            }

            return new HealthCheck(GetType());
        }
    }
}
