using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation
{
    public interface IAggregationService
    {
        LocalGame Augment(LocalGame localGame, DownloadClientItem downloadClientItem);
    }

    public class AggregationService : IAggregationService
    {
        private readonly IEnumerable<IAggregateLocalGame> _augmenters;
        private readonly IDiskProvider _diskProvider;
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public AggregationService(IEnumerable<IAggregateLocalGame> augmenters,
                                 IDiskProvider diskProvider,
                                 IVideoFileInfoReader videoFileInfoReader,
                                 IConfigService configService,
                                 Logger logger)
        {
            _augmenters = augmenters.OrderBy(a => a.Order).ToList();
            _diskProvider = diskProvider;
            _videoFileInfoReader = videoFileInfoReader;
            _configService = configService;
            _logger = logger;
        }

        public LocalGame Augment(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var isFolder = _diskProvider.FolderExists(localGame.Path);
            var isMediaFile = !isFolder && MediaFileExtensions.Extensions.Contains(Path.GetExtension(localGame.Path));

            if (localGame.DownloadClientGameInfo == null &&
                localGame.FolderGameInfo == null &&
                localGame.FileGameInfo == null)
            {
                if (isMediaFile)
                {
                    throw new AugmentingFailedException("Unable to parse game info from path: {0}", localGame.Path);
                }
            }

            // Handle folder size for game folders
            if (isFolder)
            {
                localGame.Size = _diskProvider.GetFolderSize(localGame.Path);
            }
            else
            {
                localGame.Size = _diskProvider.GetFileSize(localGame.Path);
            }

            localGame.SceneName = localGame.SceneSource ? SceneNameCalculator.GetSceneName(localGame) : null;

            // Skip media info for folders - games don't have video media info
            if (!isFolder && isMediaFile && (!localGame.ExistingFile || _configService.EnableMediaInfo))
            {
                localGame.MediaInfo = _videoFileInfoReader.GetMediaInfo(localGame.Path);
            }

            foreach (var augmenter in _augmenters)
            {
                try
                {
                    augmenter.Aggregate(localGame, downloadClientItem);
                }
                catch (Exception ex)
                {
                    var message = $"Unable to augment information for file: '{localGame.Path}'. Game: {localGame.Game} Error: {ex.Message}";

                    _logger.Warn(ex, message);
                }
            }

            return localGame;
        }
    }
}
