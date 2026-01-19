using System;
using System.IO;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Games
{
    public interface IBuildGamePaths
    {
        string BuildPath(Game game, bool useExistingRelativeFolder);
    }

    public class GamePathBuilder : IBuildGamePaths
    {
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public GamePathBuilder(IBuildFileNames fileNameBuilder, IRootFolderService rootFolderService, Logger logger)
        {
            _fileNameBuilder = fileNameBuilder;
            _rootFolderService = rootFolderService;
            _logger = logger;
        }

        public string BuildPath(Game game, bool useExistingRelativeFolder)
        {
            if (game.RootFolderPath.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Root folder was not provided", nameof(game));
            }

            if (useExistingRelativeFolder && game.Path.IsNotNullOrWhiteSpace())
            {
                var relativePath = GetExistingRelativePath(game);
                return Path.Combine(game.RootFolderPath, relativePath);
            }

            return Path.Combine(game.RootFolderPath, _fileNameBuilder.GetGameFolder(game));
        }

        private string GetExistingRelativePath(Game game)
        {
            var rootFolderPath = _rootFolderService.GetBestRootFolderPath(game.Path);

            if (rootFolderPath.IsParentPath(game.Path))
            {
                return rootFolderPath.GetRelativePath(game.Path);
            }

            var directoryName = game.Path.GetDirectoryName();

            _logger.Warn("Unable to get relative path for game path {0}, using game folder name {1}", game.Path, directoryName);

            return directoryName;
        }
    }
}
