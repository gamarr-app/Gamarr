using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NzbDrone.Common;
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
        private readonly Logger _logger;

        public MediaFileTableCleanupService(IMediaFileService mediaFileService,
                                            IGameService gameService,
                                            Logger logger)
        {
            _mediaFileService = mediaFileService;
            _gameService = gameService;
            _logger = logger;
        }

        public void Clean(Game game, List<string> filesOnDisk)
        {
            var gameFiles = _mediaFileService.GetFilesByGame(game.Id);

            var filesOnDiskKeys = new HashSet<string>(filesOnDisk, PathEqualityComparer.Instance);

            foreach (var gameFile in gameFiles)
            {
                var gameFilePath = Path.Combine(game.Path, gameFile.RelativePath);

                try
                {
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
