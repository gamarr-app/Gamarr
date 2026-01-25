using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileTableCleanupService
    {
        void Clean(Game game, List<string> filesOnDisk);
    }

    public class MediaFileTableCleanupService : IMediaFileTableCleanupService
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IGameService _gameService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public MediaFileTableCleanupService(IMediaFileService mediaFileService,
                                            IGameService gameService,
                                            IDiskProvider diskProvider,
                                            Logger logger)
        {
            _mediaFileService = mediaFileService;
            _gameService = gameService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public void Clean(Game game, List<string> filesOnDisk)
        {
            var gameFiles = _mediaFileService.GetFilesByGame(game.Id);

            var filesOnDiskKeys = new HashSet<string>(filesOnDisk, PathEqualityComparer.Instance);

            foreach (var gameFile in gameFiles)
            {
                try
                {
                    // For folder-based GameFiles (RelativePath is empty), check if folder has content
                    if (gameFile.IsFolder())
                    {
                        var folderHasContent = _diskProvider.FolderExists(game.Path) &&
                                               _diskProvider.GetFiles(game.Path, true).Any();

                        if (!folderHasContent)
                        {
                            _logger.Debug("Game folder [{0}] is empty or missing, removing from db", game.Path);
                            _mediaFileService.Delete(gameFile, DeleteMediaFileReason.MissingFromDisk);
                        }

                        continue;
                    }

                    // For file-based GameFiles, check if file exists
                    var gameFilePath = gameFile.GetPath(game);

                    if (!filesOnDiskKeys.Contains(gameFilePath))
                    {
                        _logger.Debug("File [{0}] no longer exists on disk, removing from db", gameFilePath);
                        _mediaFileService.Delete(gameFile, DeleteMediaFileReason.MissingFromDisk);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = string.Format("Unable to cleanup GameFile in DB: {0}", gameFile.Id);
                    _logger.Error(ex, errorMessage);
                }
            }
        }
    }
}
