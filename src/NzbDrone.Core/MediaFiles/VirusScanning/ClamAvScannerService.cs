using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.MediaFiles.VirusScanning
{
    public class ClamAvScannerService : IVirusScannerService
    {
        private readonly IProcessProvider _processProvider;
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        private static readonly Regex InfectedFileRegex = new Regex(@"^(.+?):\s+(.+?)\s+FOUND$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ScanSummaryRegex = new Regex(@"Scanned files:\s*(\d+)", RegexOptions.Compiled);

        // Cache the detected path to avoid repeated logging
        private string _cachedClamScanPath;
        private bool _clamScanPathChecked;

        public ClamAvScannerService(IProcessProvider processProvider,
                                    IConfigService configService,
                                    IDiskProvider diskProvider,
                                    Logger logger)
        {
            _processProvider = processProvider;
            _configService = configService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public string ScannerName => "ClamAV";

        public string DetectedScannerPath
        {
            get
            {
                try
                {
                    return GetClamScanPath();
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool IsAvailable
        {
            get
            {
                if (!_configService.VirusScanEnabled)
                {
                    return false;
                }

                var clamScanPath = GetClamScanPath();
                return clamScanPath.IsNotNullOrWhiteSpace() && _diskProvider.FileExists(clamScanPath);
            }
        }

        public VirusScanResult ScanPath(string path)
        {
            if (!IsAvailable)
            {
                return new VirusScanResult
                {
                    IsClean = true,
                    ScanCompleted = false,
                    ErrorMessage = "ClamAV is not available or not configured"
                };
            }

            var stopwatch = Stopwatch.StartNew();
            var result = new VirusScanResult();

            try
            {
                var clamScanPath = GetClamScanPath();
                var arguments = BuildArguments(path);

                _logger.Info("Starting ClamAV scan of: {0}", path);

                var processOutput = _processProvider.StartAndCapture(clamScanPath, arguments);
                var output = string.Join("\n", processOutput.Lines.Select(l => l.Content));

                stopwatch.Stop();
                result.ScanDurationMs = stopwatch.ElapsedMilliseconds;
                result.ScanCompleted = true;

                // Parse infected files
                var matches = InfectedFileRegex.Matches(output);
                foreach (Match match in matches)
                {
                    result.InfectedFiles.Add(new InfectedFile
                    {
                        FilePath = match.Groups[1].Value.Trim(),
                        ThreatName = match.Groups[2].Value.Trim()
                    });
                }

                // Parse scan summary
                var summaryMatch = ScanSummaryRegex.Match(output);
                if (summaryMatch.Success)
                {
                    result.ScannedFileCount = int.Parse(summaryMatch.Groups[1].Value);
                }

                result.IsClean = result.InfectedFiles.Count == 0;

                // Exit code 1 means virus found, 0 means clean, 2 means error
                if (processOutput.ExitCode == 2)
                {
                    result.ScanCompleted = false;
                    result.ErrorMessage = "ClamAV encountered an error during scan";
                    _logger.Warn("ClamAV scan error. Output: {0}", output);
                }
                else if (result.InfectedFiles.Any())
                {
                    _logger.Warn("ClamAV detected {0} infected file(s) in {1}", result.InfectedFiles.Count, path);
                    foreach (var infected in result.InfectedFiles)
                    {
                        _logger.Warn("  Infected: {0} - {1}", infected.FilePath, infected.ThreatName);
                    }
                }
                else
                {
                    _logger.Info("ClamAV scan completed. {0} files scanned, no threats detected", result.ScannedFileCount);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ScanDurationMs = stopwatch.ElapsedMilliseconds;
                result.ScanCompleted = false;
                result.ErrorMessage = ex.Message;
                _logger.Error(ex, "Error running ClamAV scan on {0}", path);
            }

            return result;
        }

        public VirusScanResult ScanFile(string filePath)
        {
            return ScanPath(filePath);
        }

        private string GetClamScanPath()
        {
            var configuredPath = _configService.VirusScannerPath;

            if (configuredPath.IsNotNullOrWhiteSpace())
            {
                return configuredPath;
            }

            // Return cached path if already detected
            if (_clamScanPathChecked)
            {
                return _cachedClamScanPath;
            }

            // Try common locations
            var commonPaths = new[]
            {
                "/usr/bin/clamscan",
                "/usr/local/bin/clamscan",
                "/opt/homebrew/bin/clamscan",
                @"C:\Program Files\ClamAV\clamscan.exe",
                @"C:\Program Files (x86)\ClamAV\clamscan.exe"
            };

            var detectedPath = commonPaths.FirstOrDefault(p => _diskProvider.FileExists(p));

            if (detectedPath != null)
            {
                _logger.Info("Auto-detected ClamAV at: {0}", detectedPath);
            }
            else
            {
                _logger.Debug("ClamAV not found in common locations");
            }

            _cachedClamScanPath = detectedPath;
            _clamScanPathChecked = true;

            return detectedPath;
        }

        private string BuildArguments(string path)
        {
            var args = new List<string>
            {
                "--recursive",
                "--infected",
                "--no-summary=false"
            };

            // Add configured arguments if any
            var extraArgs = _configService.VirusScannerArguments;
            if (extraArgs.IsNotNullOrWhiteSpace())
            {
                args.Add(extraArgs);
            }

            // Escape any quotes in the path and wrap in quotes for safe argument passing
            args.Add($"\"{path.Replace("\"", "\\\"")}\"");

            return string.Join(" ", args);
        }
    }
}
