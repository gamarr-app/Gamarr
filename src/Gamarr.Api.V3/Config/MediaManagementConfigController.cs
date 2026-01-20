using FluentValidation;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.VirusScanning;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using Gamarr.Http;

namespace Gamarr.Api.V3.Config
{
    [V3ApiController("config/mediamanagement")]
    public class MediaManagementConfigController : ConfigController<MediaManagementConfigResource>
    {
        private readonly IVirusScannerService _virusScannerService;

        public MediaManagementConfigController(IConfigService configService,
                                           IVirusScannerService virusScannerService,
                                           PathExistsValidator pathExistsValidator,
                                           FolderChmodValidator folderChmodValidator,
                                           FolderWritableValidator folderWritableValidator,
                                           GamePathValidator gamePathValidator,
                                           StartupFolderValidator startupFolderValidator,
                                           SystemFolderValidator systemFolderValidator,
                                           RootFolderAncestorValidator rootFolderAncestorValidator,
                                           RootFolderValidator rootFolderValidator)
            : base(configService)
        {
            _virusScannerService = virusScannerService;

            SharedValidator.RuleFor(c => c.RecycleBinCleanupDays).GreaterThanOrEqualTo(0);
            SharedValidator.RuleFor(c => c.ChmodFolder).SetValidator(folderChmodValidator).When(c => !string.IsNullOrEmpty(c.ChmodFolder) && (OsInfo.IsLinux || OsInfo.IsOsx));

            SharedValidator.RuleFor(c => c.RecycleBin).IsValidPath()
                                                      .SetValidator(folderWritableValidator)
                                                      .SetValidator(rootFolderValidator)
                                                      .SetValidator(pathExistsValidator)
                                                      .SetValidator(rootFolderAncestorValidator)
                                                      .SetValidator(startupFolderValidator)
                                                      .SetValidator(systemFolderValidator)
                                                      .SetValidator(gamePathValidator)
                                                      .When(c => !string.IsNullOrWhiteSpace(c.RecycleBin));

            SharedValidator.RuleFor(c => c.ScriptImportPath).IsValidPath().When(c => c.UseScriptImport);

            SharedValidator.RuleFor(c => c.MinimumFreeSpaceWhenImporting).GreaterThanOrEqualTo(100);
        }

        protected override MediaManagementConfigResource ToResource(IConfigService model)
        {
            var resource = MediaManagementConfigResourceMapper.ToResource(model);
            resource.DetectedVirusScannerPath = _virusScannerService.DetectedScannerPath;
            return resource;
        }
    }
}
