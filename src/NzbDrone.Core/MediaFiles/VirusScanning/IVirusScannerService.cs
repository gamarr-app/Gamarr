namespace NzbDrone.Core.MediaFiles.VirusScanning
{
    public interface IVirusScannerService
    {
        bool IsAvailable { get; }
        string ScannerName { get; }
        VirusScanResult ScanPath(string path);
        VirusScanResult ScanFile(string filePath);
    }
}
