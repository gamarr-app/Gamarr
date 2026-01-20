namespace NzbDrone.Core.MediaFiles.VirusScanning
{
    public interface IVirusScannerService
    {
        bool IsAvailable { get; }
        string ScannerName { get; }
        string DetectedScannerPath { get; }
        VirusScanResult ScanPath(string path);
        VirusScanResult ScanFile(string filePath);
    }
}
