using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles.VirusScanning
{
    public class VirusScanResult
    {
        public bool IsClean { get; set; }
        public bool ScanCompleted { get; set; }
        public string ErrorMessage { get; set; }
        public List<InfectedFile> InfectedFiles { get; set; } = new List<InfectedFile>();
        public int ScannedFileCount { get; set; }
        public long ScanDurationMs { get; set; }
    }

    public class InfectedFile
    {
        public string FilePath { get; set; }
        public string ThreatName { get; set; }
    }
}
