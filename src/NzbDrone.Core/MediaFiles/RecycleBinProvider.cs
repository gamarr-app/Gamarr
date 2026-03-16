using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRecycleBinProvider
    {
        void DeleteFolder(string path);
        string DeleteFile(string path, string subfolder = "");
        void Empty();
        void Cleanup();
    }

    public class RecycleBinProvider : IExecute<CleanUpRecycleBinCommand>, IRecycleBinProvider
    {
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public RecycleBinProvider(IDiskTransferService diskTransferService,
                                  IDiskProvider diskProvider,
                                  IConfigService configService,
                                  IRootFolderService rootFolderService,
                                  Logger logger)
        {
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _configService = configService;
            _rootFolderService = rootFolderService;
            _logger = logger;
        }

        private string GetRecycleBinForPath(string path)
        {
            var recyclingBin = _configService.RecycleBin;

            if (recyclingBin.IsNotNullOrWhiteSpace())
            {
                return recyclingBin;
            }

            // Derive a .recycle folder from the file's root folder
            var rootFolderPath = _rootFolderService.GetBestRootFolderPath(path);

            if (rootFolderPath.IsNotNullOrWhiteSpace())
            {
                return Path.Combine(rootFolderPath, ".recycle");
            }

            return null;
        }

        public void DeleteFolder(string path)
        {
            _logger.Info("Attempting to send '{0}' to recycling bin", path);
            var recyclingBin = GetRecycleBinForPath(path);

            if (string.IsNullOrWhiteSpace(recyclingBin))
            {
                _logger.Info("No root folder found for '{0}', deleting permanently.", path);
                _diskProvider.DeleteFolder(path, true);
                _logger.Debug("Folder has been permanently deleted: {0}", path);
            }
            else
            {
                var destination = Path.Combine(recyclingBin, new DirectoryInfo(path).Name);

                _logger.Debug("Moving '{0}' to '{1}'", path, destination);
                _diskTransferService.TransferFolder(path, destination, TransferMode.Move);

                _logger.Debug("Setting last accessed: {0}", path);
                _diskProvider.FolderSetLastWriteTime(destination, DateTime.UtcNow);
                foreach (var file in _diskProvider.GetFiles(destination, true))
                {
                    SetLastWriteTime(file, DateTime.UtcNow);
                }

                _logger.Debug("Folder has been moved to the recycling bin: {0}", destination);
            }
        }

        public string DeleteFile(string path, string subfolder = "")
        {
            _logger.Debug("Attempting to send '{0}' to recycling bin", path);
            var recyclingBin = GetRecycleBinForPath(path);

            if (string.IsNullOrWhiteSpace(recyclingBin))
            {
                _logger.Info("No root folder found for '{0}', deleting permanently.", path);

                if (OsInfo.IsWindows)
                {
                    _logger.Debug(_diskProvider.GetFileAttributes(path));
                }

                _diskProvider.DeleteFile(path);
                _logger.Debug("File has been permanently deleted: {0}", path);

                return null;
            }
            else
            {
                var fileInfo = new FileInfo(path);
                var destinationFolder = Path.Combine(recyclingBin, subfolder);
                var destination = Path.Combine(destinationFolder, fileInfo.Name);

                try
                {
                    _logger.Debug("Creating folder {0}", destinationFolder);
                    _diskProvider.CreateFolder(destinationFolder);
                }
                catch (IOException e)
                {
                    _logger.Error(e, "Unable to create the folder '{0}' in the recycling bin for the file '{1}'", destinationFolder, fileInfo.Name);
                    throw new RecycleBinException($"Unable to create the folder '{destinationFolder}' in the recycling bin for the file '{fileInfo.Name}'", e);
                }

                var index = 1;
                while (_diskProvider.FileExists(destination))
                {
                    index++;
                    if (fileInfo.Extension.IsNullOrWhiteSpace())
                    {
                        destination = Path.Combine(destinationFolder, fileInfo.Name + "_" + index);
                    }
                    else
                    {
                        destination = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fileInfo.Name) + "_" + index + fileInfo.Extension);
                    }
                }

                try
                {
                    _logger.Debug("Moving '{0}' to '{1}'", path, destination);
                    _diskTransferService.TransferFile(path, destination, TransferMode.Move);
                }
                catch (IOException e)
                {
                    _logger.Error(e, "Unable to move '{0}' to the recycling bin: '{1}'", path, destination);
                    throw new RecycleBinException($"Unable to move '{path}' to the recycling bin: '{destination}'", e);
                }

                SetLastWriteTime(destination, DateTime.UtcNow);

                _logger.Debug("File has been moved to the recycling bin: {0}", destination);

                return destination;
            }
        }

        private List<string> GetAllRecycleBinPaths()
        {
            var paths = new List<string>();
            var globalBin = _configService.RecycleBin;

            if (globalBin.IsNotNullOrWhiteSpace())
            {
                paths.Add(globalBin);
            }
            else
            {
                foreach (var rootFolder in _rootFolderService.All())
                {
                    paths.Add(Path.Combine(rootFolder.Path, ".recycle"));
                }
            }

            return paths;
        }

        public void Empty()
        {
            var recycleBinPaths = GetAllRecycleBinPaths();

            if (recycleBinPaths.Count == 0)
            {
                _logger.Info("No recycle bin paths found, cannot empty.");
                return;
            }

            _logger.Info("Removing all items from the recycling bin");

            foreach (var recycleBin in recycleBinPaths)
            {
                if (!_diskProvider.FolderExists(recycleBin))
                {
                    continue;
                }

                foreach (var folder in _diskProvider.GetDirectories(recycleBin))
                {
                    _diskProvider.DeleteFolder(folder, true);
                }

                foreach (var file in _diskProvider.GetFiles(recycleBin, false))
                {
                    _diskProvider.DeleteFile(file);
                }
            }

            _logger.Debug("Recycling Bin has been emptied.");
        }

        public void Cleanup()
        {
            var recycleBinPaths = GetAllRecycleBinPaths();

            if (recycleBinPaths.Count == 0)
            {
                _logger.Info("No recycle bin paths found, cannot cleanup.");
                return;
            }

            var cleanupDays = _configService.RecycleBinCleanupDays;

            if (cleanupDays == 0)
            {
                _logger.Info("Automatic cleanup of Recycle Bin is disabled");
                return;
            }

            _logger.Info("Removing items older than {0} days from the recycling bin", cleanupDays);

            foreach (var recycleBin in recycleBinPaths)
            {
                if (!_diskProvider.FolderExists(recycleBin))
                {
                    continue;
                }

                foreach (var file in _diskProvider.GetFiles(recycleBin, true))
                {
                    if (_diskProvider.FileGetLastWrite(file).AddDays(cleanupDays) > DateTime.UtcNow)
                    {
                        _logger.Debug("File hasn't expired yet, skipping: {0}", file);
                        continue;
                    }

                    try
                    {
                        _diskProvider.DeleteFile(file);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.Error(ex.Message);
                        continue;
                    }
                }

                _diskProvider.RemoveEmptySubfolders(recycleBin);
            }

            _logger.Debug("Recycling Bin has been cleaned up.");
        }

        private void SetLastWriteTime(string file, DateTime dateTime)
        {
            // Swallow any IOException that may be thrown due to "Invalid parameter"
            try
            {
                _diskProvider.FileSetLastWriteTime(file, dateTime);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public void Execute(CleanUpRecycleBinCommand message)
        {
            Cleanup();
        }
    }
}
