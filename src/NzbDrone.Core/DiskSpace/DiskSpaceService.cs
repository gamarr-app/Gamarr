using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.DiskSpace
{
    public interface IDiskSpaceService
    {
        List<DiskSpace> GetFreeSpace();
    }

    public class DiskSpaceService : IDiskSpaceService
    {
        private readonly IGameService _gameService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        private static readonly Regex _regexSpecialDrive = new Regex(@"^/var/lib/(docker|rancher|kubelet)(/|$)|^/(boot|etc)(/|$)|/docker(/var)?/aufs(/|$)|/\.timemachine", RegexOptions.Compiled);

        public DiskSpaceService(IGameService gameService, IRootFolderService rootFolderService, IDiskProvider diskProvider, Logger logger)
        {
            _gameService = gameService;
            _rootFolderService = rootFolderService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<DiskSpace> GetFreeSpace()
        {
            var importantRootFolders = GetGamesRootPaths().Distinct().ToList();

            var optionalRootFolders = GetFixedDisksRootPaths().Except(importantRootFolders).Distinct().ToList();

            var diskSpace = GetDiskSpace(importantRootFolders)
                .Concat(GetDiskSpace(optionalRootFolders, true))
                .OrderBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return diskSpace;
        }

        private IEnumerable<string> GetGamesRootPaths()
        {
            // Get all game paths and find the correct root folder for each. For each unique root folder path,
            // ensure the path exists and get its path root and return all unique path roots.

            return _gameService.AllGamePaths()
                .Where(s => s.Value.IsPathValid(PathValidationType.CurrentOs))
                .Select(s => _rootFolderService.GetBestRootFolderPath(s.Value))
                .Distinct()
                .Where(r => _diskProvider.FolderExists(r))
                .Select(r => _diskProvider.GetPathRoot(r))
                .Distinct();
        }

        private IEnumerable<string> GetFixedDisksRootPaths()
        {
            return _diskProvider.GetMounts()
                .Where(d => d.DriveType is DriveType.Fixed or DriveType.Network)
                .Where(d => !_regexSpecialDrive.IsMatch(d.RootDirectory))
                .Select(d => d.RootDirectory);
        }

        private IEnumerable<DiskSpace> GetDiskSpace(IEnumerable<string> paths, bool suppressWarnings = false)
        {
            foreach (var path in paths)
            {
                DiskSpace diskSpace = null;

                try
                {
                    var freeSpace = _diskProvider.GetAvailableSpace(path);
                    var totalSpace = _diskProvider.GetTotalSize(path);

                    if (!freeSpace.HasValue || !totalSpace.HasValue)
                    {
                        continue;
                    }

                    diskSpace = new DiskSpace
                    {
                        Path = path,
                        FreeSpace = freeSpace.Value,
                        TotalSpace = totalSpace.Value
                    };

                    diskSpace.Label = _diskProvider.GetVolumeLabel(path);
                }
                catch (Exception ex)
                {
                    if (!suppressWarnings)
                    {
                        _logger.Warn(ex, "Unable to get free space for: " + path);
                    }
                }

                if (diskSpace != null)
                {
                    yield return diskSpace;
                }
            }
        }
    }
}
