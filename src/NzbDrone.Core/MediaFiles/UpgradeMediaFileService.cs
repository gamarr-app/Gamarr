using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpgradeMediaFiles
    {
        GameFileMoveResult UpgradeGameFile(GameFile gameFile, LocalGame localGame, bool copyOnly = false);
    }

    public class UpgradeMediaFileService : IUpgradeMediaFiles
    {
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMoveGameFiles _gameFileMover;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public UpgradeMediaFileService(IRecycleBinProvider recycleBinProvider,
                                       IMediaFileService mediaFileService,
                                       IMoveGameFiles gameFileMover,
                                       IDiskProvider diskProvider,
                                       Logger logger)
        {
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _gameFileMover = gameFileMover;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public GameFileMoveResult UpgradeGameFile(GameFile gameFile, LocalGame localGame, bool copyOnly = false)
        {
            _logger.Trace("Upgrading game file.");

            var moveFileResult = new GameFileMoveResult();

            var existingFile = localGame.Game.GameFileId > 0 ? localGame.Game.GameFile : null;

            var rootFolder = _diskProvider.GetParentFolder(localGame.Game.Path);

            // If there are existing game files and the root folder is missing, throw, so the old file isn't left behind during the import process.
            if (existingFile != null && !_diskProvider.FolderExists(rootFolder))
            {
                throw new RootFolderNotFoundException($"Root folder '{rootFolder}' was not found.");
            }

            if (existingFile != null)
            {
                string recycleBinPath = null;

                // Handle folder-based GameFiles (RelativePath is empty)
                if (existingFile.IsFolder())
                {
                    if (_diskProvider.FolderExists(localGame.Game.Path) &&
                        _diskProvider.GetFiles(localGame.Game.Path, true).Any())
                    {
                        _logger.Debug("Removing existing game folder contents: {0}", localGame.Game.Path);
                        _recycleBinProvider.DeleteFolder(localGame.Game.Path);
                    }
                    else
                    {
                        _logger.Warn("Existing game folder empty or missing: {0}", localGame.Game.Path);
                    }
                }
                else
                {
                    var gameFilePath = existingFile.GetPath(localGame.Game);
                    var subfolder = rootFolder.GetRelativePath(_diskProvider.GetParentFolder(gameFilePath));

                    if (_diskProvider.FileExists(gameFilePath))
                    {
                        _logger.Debug("Removing existing game file: {0}", existingFile);
                        recycleBinPath = _recycleBinProvider.DeleteFile(gameFilePath, subfolder);
                    }
                    else
                    {
                        _logger.Warn("Existing game file missing from disk: {0}", gameFilePath);
                    }
                }

                moveFileResult.OldFiles.Add(new DeletedGameFile(existingFile, recycleBinPath));
                _mediaFileService.Delete(existingFile, DeleteMediaFileReason.Upgrade);
            }

            localGame.OldFiles = moveFileResult.OldFiles;

            if (copyOnly)
            {
                moveFileResult.GameFile = _gameFileMover.CopyGameFile(gameFile, localGame);
            }
            else
            {
                moveFileResult.GameFile = _gameFileMover.MoveGameFile(gameFile, localGame);
            }

            return moveFileResult;
        }
    }
}
