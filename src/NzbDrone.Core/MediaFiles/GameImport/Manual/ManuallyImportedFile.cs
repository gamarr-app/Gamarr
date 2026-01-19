using NzbDrone.Core.Download.TrackedDownloads;

namespace NzbDrone.Core.MediaFiles.GameImport.Manual
{
    public class ManuallyImportedFile
    {
        public TrackedDownload TrackedDownload { get; set; }
        public ImportResult ImportResult { get; set; }
    }
}
