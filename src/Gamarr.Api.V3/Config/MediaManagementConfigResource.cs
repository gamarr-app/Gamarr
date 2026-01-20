using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Config
{
    public class MediaManagementConfigResource : RestResource
    {
        public bool AutoUnmonitorPreviouslyDownloadedGames { get; set; }
        public string RecycleBin { get; set; }
        public int RecycleBinCleanupDays { get; set; }
        public ProperDownloadTypes DownloadPropersAndRepacks { get; set; }
        public bool CreateEmptyGameFolders { get; set; }
        public bool DeleteEmptyFolders { get; set; }
        public FileDateType FileDate { get; set; }
        public RescanAfterRefreshType RescanAfterRefresh { get; set; }
        public bool AutoRenameFolders { get; set; }
        public bool PathsDefaultStatic { get; set; }

        public bool SetPermissionsLinux { get; set; }
        public string ChmodFolder { get; set; }
        public string ChownGroup { get; set; }

        public bool SkipFreeSpaceCheckWhenImporting { get; set; }
        public int MinimumFreeSpaceWhenImporting { get; set; }
        public bool CopyUsingHardlinks { get; set; }
        public bool UseScriptImport { get; set; }
        public string ScriptImportPath { get; set; }
        public bool ImportExtraFiles { get; set; }
        public string ExtraFileExtensions { get; set; }
        public bool EnableMediaInfo { get; set; }

        public bool VirusScanEnabled { get; set; }
        public string VirusScannerPath { get; set; }
        public string VirusScannerArguments { get; set; }
        public bool QuarantineInfectedFiles { get; set; }
        public string QuarantineFolder { get; set; }
        public string DetectedVirusScannerPath { get; set; }
    }

    public static class MediaManagementConfigResourceMapper
    {
        public static MediaManagementConfigResource ToResource(IConfigService model)
        {
            return new MediaManagementConfigResource
            {
                AutoUnmonitorPreviouslyDownloadedGames = model.AutoUnmonitorPreviouslyDownloadedGames,
                RecycleBin = model.RecycleBin,
                RecycleBinCleanupDays = model.RecycleBinCleanupDays,
                DownloadPropersAndRepacks = model.DownloadPropersAndRepacks,
                CreateEmptyGameFolders = model.CreateEmptyGameFolders,
                DeleteEmptyFolders = model.DeleteEmptyFolders,
                FileDate = model.FileDate,
                RescanAfterRefresh = model.RescanAfterRefresh,
                AutoRenameFolders = model.AutoRenameFolders,

                SetPermissionsLinux = model.SetPermissionsLinux,
                ChmodFolder = model.ChmodFolder,
                ChownGroup = model.ChownGroup,

                SkipFreeSpaceCheckWhenImporting = model.SkipFreeSpaceCheckWhenImporting,
                MinimumFreeSpaceWhenImporting = model.MinimumFreeSpaceWhenImporting,
                CopyUsingHardlinks = model.CopyUsingHardlinks,
                UseScriptImport = model.UseScriptImport,
                ScriptImportPath = model.ScriptImportPath,
                ImportExtraFiles = model.ImportExtraFiles,
                ExtraFileExtensions = model.ExtraFileExtensions,
                EnableMediaInfo = model.EnableMediaInfo,

                VirusScanEnabled = model.VirusScanEnabled,
                VirusScannerPath = model.VirusScannerPath,
                VirusScannerArguments = model.VirusScannerArguments,
                QuarantineInfectedFiles = model.QuarantineInfectedFiles,
                QuarantineFolder = model.QuarantineFolder
            };
        }
    }
}
