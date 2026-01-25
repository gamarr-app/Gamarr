using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDiskScanService
    {
        void Scan(Game game);
        string[] GetVideoFiles(string path, bool allDirectories = true);
        string[] GetNonVideoFiles(string path, bool allDirectories = true);
        List<string> FilterPaths(string basePath, IEnumerable<string> paths, bool filterExtras = true);
    }

    public class DiskScanService :
        IDiskScanService,
        IExecute<RescanGameCommand>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedGame _importApprovedGames;
        private readonly IConfigService _configService;
        private readonly IGameService _gameService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMediaFileTableCleanupService _mediaFileTableCleanupService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IUpdateMediaInfo _updateMediaInfoService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public DiskScanService(IDiskProvider diskProvider,
                               IMakeImportDecision importDecisionMaker,
                               IImportApprovedGame importApprovedGames,
                               IConfigService configService,
                               IGameService gameService,
                               IMediaFileService mediaFileService,
                               IMediaFileTableCleanupService mediaFileTableCleanupService,
                               IRootFolderService rootFolderService,
                               IUpdateMediaInfo updateMediaInfoService,
                               IEventAggregator eventAggregator,
                               Logger logger)
        {
            _diskProvider = diskProvider;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedGames = importApprovedGames;
            _configService = configService;
            _gameService = gameService;
            _mediaFileService = mediaFileService;
            _mediaFileTableCleanupService = mediaFileTableCleanupService;
            _rootFolderService = rootFolderService;
            _updateMediaInfoService = updateMediaInfoService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        private static readonly Regex ExcludedExtrasSubFolderRegex = new Regex(@"(?:\\|\/|^)(?:extras|extrafanart|behind the scenes|deleted scenes|featurettes|interviews|other|scenes|sample[s]?|shorts|trailers)(?:\\|\/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExcludedSubFoldersRegex = new Regex(@"(?:\\|\/|^)(?:@eadir|\.@__thumb|plex versions|\.[^\\/]+)(?:\\|\/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExcludedExtraFilesRegex = new Regex(@"(-(trailer|other|behindthescenes|deleted|featurette|interview|scene|short)\.[^.]+$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExcludedFilesRegex = new Regex(@"^\.(_|unmanic|DS_Store$)|^Thumbs\.db$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public void Scan(Game game)
        {
            var rootFolder = _rootFolderService.GetBestRootFolderPath(game.Path);

            var gameFolderExists = _diskProvider.FolderExists(game.Path);

            if (!gameFolderExists)
            {
                if (!_diskProvider.FolderExists(rootFolder))
                {
                    _logger.Warn("Game's root folder ({0}) doesn't exist.", rootFolder);
                    _eventAggregator.PublishEvent(new GameScanSkippedEvent(game, GameScanSkippedReason.RootFolderDoesNotExist));
                    return;
                }

                if (_diskProvider.FolderEmpty(rootFolder))
                {
                    _logger.Warn("Game's root folder ({0}) is empty. Rescan will not update games as a failsafe.", rootFolder);
                    _eventAggregator.PublishEvent(new GameScanSkippedEvent(game, GameScanSkippedReason.RootFolderIsEmpty));
                    return;
                }
            }

            _logger.ProgressInfo("Scanning disk for {0}", game.Title);

            if (!gameFolderExists)
            {
                if (_configService.CreateEmptyGameFolders)
                {
                    if (_configService.DeleteEmptyFolders)
                    {
                        _logger.Debug("Not creating missing game folder: {0} because delete empty game folders is enabled", game.Path);
                    }
                    else
                    {
                        _logger.Debug("Creating missing game folder: {0}", game.Path);

                        _diskProvider.CreateFolder(game.Path);
                        SetPermissions(game.Path);
                    }
                }
                else
                {
                    _logger.Debug("Game's folder doesn't exist: {0}", game.Path);
                }

                CleanMediaFiles(game, new List<string>());
                CompletedScanning(game, new List<string>());

                return;
            }

            // Check if the game folder has any content
            var filesInFolder = _diskProvider.GetFiles(game.Path, true);
            var folderHasContent = filesInFolder.Any();

            // Get existing game files from database
            var existingGameFiles = _mediaFileService.GetFilesByGame(game.Id);

            if (folderHasContent)
            {
                // Folder has content - treat the entire folder as a single GameFile
                var folderSize = _diskProvider.GetFolderSize(game.Path);

                // Check if we already have a folder-based GameFile (RelativePath is empty)
                var existingFolderFile = existingGameFiles.FirstOrDefault(f => f.RelativePath.IsNullOrWhiteSpace());

                if (existingFolderFile != null)
                {
                    // Update existing folder record if size changed
                    if (existingFolderFile.Size != folderSize)
                    {
                        _logger.Debug("Updating folder size for {0}: {1} -> {2}", game.Title, existingFolderFile.Size, folderSize);
                        existingFolderFile.Size = folderSize;
                        _mediaFileService.Update(existingFolderFile);
                    }
                }
                else
                {
                    // No folder-based GameFile exists - create one
                    // First, delete any old file-based GameFile records (migration from old behavior)
                    foreach (var oldFile in existingGameFiles)
                    {
                        _logger.Debug("Removing old file-based GameFile record: {0}", oldFile.RelativePath);
                        _mediaFileService.Delete(oldFile, DeleteMediaFileReason.ManualOverride);
                    }

                    // Create new folder-based GameFile
                    _logger.Debug("Creating folder-based GameFile for {0}", game.Title);
                    var folderGameFile = new GameFile
                    {
                        GameId = game.Id,
                        RelativePath = string.Empty, // Empty means the folder itself
                        Size = folderSize,
                        DateAdded = DateTime.UtcNow,
                        Quality = new QualityModel { Quality = Quality.Unknown },
                        Languages = new List<Language> { Language.Unknown },
                        IndexerFlags = 0
                    };

                    _mediaFileService.Add(folderGameFile);
                }
            }
            else
            {
                // Folder is empty - clean up any existing GameFile records
                foreach (var existingFile in existingGameFiles)
                {
                    _logger.Debug("Removing GameFile for empty folder: {0}", existingFile.RelativePath);
                    _mediaFileService.Delete(existingFile, DeleteMediaFileReason.MissingFromDisk);
                }
            }

            var filesOnDisk = GetNonVideoFiles(game.Path);
            var possibleExtraFiles = FilterPaths(game.Path, filesOnDisk);

            RemoveEmptyGameFolder(game.Path);
            CompletedScanning(game, possibleExtraFiles);
        }

        private void CleanMediaFiles(Game game, List<string> mediaFileList)
        {
            _logger.Debug("{0} Cleaning up media files in DB", game);
            _mediaFileTableCleanupService.Clean(game, mediaFileList);
        }

        private void CompletedScanning(Game game, List<string> possibleExtraFiles)
        {
            _logger.Info("Completed scanning disk for {0}", game.Title);
            _eventAggregator.PublishEvent(new GameScannedEvent(game, possibleExtraFiles));
        }

        public string[] GetVideoFiles(string path, bool allDirectories = true)
        {
            _logger.Debug("Scanning '{0}' for video files", path);

            var filesOnDisk = _diskProvider.GetFiles(path, allDirectories).ToList();

            var mediaFileList = filesOnDisk.Where(file => MediaFileExtensions.Extensions.Contains(Path.GetExtension(file)))
                                           .ToList();

            _logger.Trace("{0} files were found in {1}", filesOnDisk.Count, path);
            _logger.Debug("{0} video files were found in {1}", mediaFileList.Count, path);

            return mediaFileList.ToArray();
        }

        public string[] GetNonVideoFiles(string path, bool allDirectories = true)
        {
            _logger.Debug("Scanning '{0}' for non-video files", path);

            var filesOnDisk = _diskProvider.GetFiles(path, allDirectories).ToList();

            var mediaFileList = filesOnDisk.Where(file => !MediaFileExtensions.Extensions.Contains(Path.GetExtension(file)))
                                           .ToList();

            _logger.Trace("{0} files were found in {1}", filesOnDisk.Count, path);
            _logger.Debug("{0} non-video files were found in {1}", mediaFileList.Count, path);

            return mediaFileList.ToArray();
        }

        public List<string> FilterPaths(string basePath, IEnumerable<string> paths, bool filterExtras = true)
        {
            var filteredPaths =  paths.Where(path => !ExcludedSubFoldersRegex.IsMatch(basePath.GetRelativePath(path)))
                                      .Where(path => !ExcludedFilesRegex.IsMatch(Path.GetFileName(path)))
                                      .ToList();

            if (filterExtras)
            {
                filteredPaths = filteredPaths.Where(path => !ExcludedExtrasSubFolderRegex.IsMatch(basePath.GetRelativePath(path)))
                                             .Where(path => !ExcludedExtraFilesRegex.IsMatch(Path.GetFileName(path)))
                                             .ToList();
            }

            return filteredPaths;
        }

        private void SetPermissions(string path)
        {
            if (!_configService.SetPermissionsLinux)
            {
                return;
            }

            try
            {
                _diskProvider.SetPermissions(path, _configService.ChmodFolder, _configService.ChownGroup);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to apply permissions to: " + path);
                _logger.Debug(ex, ex.Message);
            }
        }

        private void RemoveEmptyGameFolder(string path)
        {
            if (_configService.DeleteEmptyFolders)
            {
                _diskProvider.RemoveEmptySubfolders(path);

                if (_diskProvider.FolderEmpty(path))
                {
                    _diskProvider.DeleteFolder(path, true);
                }
            }
        }

        public void Execute(RescanGameCommand message)
        {
            if (message.GameId.HasValue)
            {
                var game = _gameService.GetGame(message.GameId.Value);
                Scan(game);
            }
            else
            {
                var allGames = _gameService.GetAllGames();

                foreach (var game in allGames)
                {
                    Scan(game);
                }
            }
        }
    }
}
