using System;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMoveGameFiles
    {
        GameFile MoveGameFile(GameFile gameFile, Game game);
        GameFile MoveGameFile(GameFile gameFile, LocalGame localGame);
        GameFile CopyGameFile(GameFile gameFile, LocalGame localGame);
    }

    public class GameFileMovingService : IMoveGameFiles
    {
        private readonly IUpdateGameFileService _updateGameFileService;
        private readonly IBuildFileNames _buildFileNames;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IImportScript _scriptImportDecider;
        private readonly IRootFolderService _rootFolderService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public GameFileMovingService(IUpdateGameFileService updateGameFileService,
                                IBuildFileNames buildFileNames,
                                IDiskTransferService diskTransferService,
                                IDiskProvider diskProvider,
                                IMediaFileAttributeService mediaFileAttributeService,
                                IImportScript scriptImportDecider,
                                IRootFolderService rootFolderService,
                                IEventAggregator eventAggregator,
                                IConfigService configService,
                                Logger logger)
        {
            _updateGameFileService = updateGameFileService;
            _buildFileNames = buildFileNames;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _mediaFileAttributeService = mediaFileAttributeService;
            _scriptImportDecider = scriptImportDecider;
            _rootFolderService = rootFolderService;
            _eventAggregator = eventAggregator;
            _configService = configService;
            _logger = logger;
        }

        public GameFile MoveGameFile(GameFile gameFile, Game game)
        {
            var newFileName = _buildFileNames.BuildFileName(game, gameFile);
            var filePath = _buildFileNames.BuildFilePath(game, newFileName, Path.GetExtension(gameFile.RelativePath));

            EnsureGameFolder(gameFile, game, filePath);

            _logger.Debug("Renaming game file: {0} to {1}", gameFile, filePath);

            return TransferFile(gameFile, game, filePath, TransferMode.Move);
        }

        public GameFile MoveGameFile(GameFile gameFile, LocalGame localGame)
        {
            var newFileName = _buildFileNames.BuildFileName(localGame.Game, gameFile, null, localGame.CustomFormats);
            var filePath = _buildFileNames.BuildFilePath(localGame.Game, newFileName, Path.GetExtension(localGame.Path));

            EnsureGameFolder(gameFile, localGame, filePath);

            _logger.Debug("Moving game file: {0} to {1}", gameFile.Path, filePath);

            return TransferFile(gameFile, localGame.Game, filePath, TransferMode.Move, localGame);
        }

        public GameFile CopyGameFile(GameFile gameFile, LocalGame localGame)
        {
            var newFileName = _buildFileNames.BuildFileName(localGame.Game, gameFile, null, localGame.CustomFormats);
            var filePath = _buildFileNames.BuildFilePath(localGame.Game, newFileName, Path.GetExtension(localGame.Path));

            EnsureGameFolder(gameFile, localGame, filePath);

            if (_configService.CopyUsingHardlinks)
            {
                _logger.Debug("Attempting to hardlink game file: {0} to {1}", gameFile.Path, filePath);
                return TransferFile(gameFile, localGame.Game, filePath, TransferMode.HardLinkOrCopy, localGame);
            }

            _logger.Debug("Copying game file: {0} to {1}", gameFile.Path, filePath);
            return TransferFile(gameFile, localGame.Game, filePath, TransferMode.Copy, localGame);
        }

        private GameFile TransferFile(GameFile gameFile, Game game, string destinationFilePath, TransferMode mode, LocalGame localGame = null)
        {
            Ensure.That(gameFile, () => gameFile).IsNotNull();
            Ensure.That(game, () => game).IsNotNull();
            Ensure.That(destinationFilePath, () => destinationFilePath).IsValidPath(PathValidationType.CurrentOs);

            var gameFilePath = gameFile.Path ?? Path.Combine(game.Path, gameFile.RelativePath);

            if (!_diskProvider.FileExists(gameFilePath))
            {
                throw new FileNotFoundException("Game file path does not exist", gameFilePath);
            }

            if (gameFilePath == destinationFilePath)
            {
                throw new SameFilenameException("File not moved, source and destination are the same", gameFilePath);
            }

            gameFile.RelativePath = game.Path.GetRelativePath(destinationFilePath);

            if (localGame is not null)
            {
                localGame.FileNameBeforeRename = gameFile.RelativePath;
            }

            if (localGame is not null && _scriptImportDecider.TryImport(gameFilePath, destinationFilePath, localGame, gameFile, mode) is var scriptImportDecision && scriptImportDecision != ScriptImportDecision.DeferMove)
            {
                if (scriptImportDecision == ScriptImportDecision.RenameRequested)
                {
                    try
                    {
                        MoveGameFile(gameFile, game);
                    }
                    catch (SameFilenameException)
                    {
                        _logger.Debug("No rename was required. File already exists at destination.");
                    }
                }
            }
            else
            {
                _diskTransferService.TransferFile(gameFilePath, destinationFilePath, mode);
            }

            _updateGameFileService.ChangeFileDateForFile(gameFile, game);

            try
            {
                _mediaFileAttributeService.SetFolderLastWriteTime(game.Path, gameFile.DateAdded);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to set last write time");
            }

            _mediaFileAttributeService.SetFilePermissions(destinationFilePath);

            return gameFile;
        }

        private void EnsureGameFolder(GameFile gameFile, LocalGame localGame, string filePath)
        {
            EnsureGameFolder(gameFile, localGame.Game, filePath);
        }

        private void EnsureGameFolder(GameFile gameFile, Game game, string filePath)
        {
            var gameFileFolder = Path.GetDirectoryName(filePath);
            var gameFolder = game.Path;
            var rootFolder = _rootFolderService.GetBestRootFolderPath(gameFolder);

            if (rootFolder.IsNullOrWhiteSpace())
            {
                throw new RootFolderNotFoundException($"Root folder was not found, '{gameFolder}' is not a subdirectory of a defined root folder.");
            }

            if (!_diskProvider.FolderExists(rootFolder))
            {
                throw new RootFolderNotFoundException($"Root folder '{rootFolder}' was not found.");
            }

            var changed = false;
            var newEvent = new GameFolderCreatedEvent(game, gameFile);

            if (!_diskProvider.FolderExists(gameFolder))
            {
                CreateFolder(gameFolder);
                newEvent.GameFolder = gameFolder;
                changed = true;
            }

            if (gameFolder != gameFileFolder && !_diskProvider.FolderExists(gameFileFolder))
            {
                CreateFolder(gameFileFolder);
                newEvent.GameFileFolder = gameFileFolder;
                changed = true;
            }

            if (changed)
            {
                _eventAggregator.PublishEvent(newEvent);
            }
        }

        private void CreateFolder(string directoryName)
        {
            Ensure.That(directoryName, () => directoryName).IsNotNullOrWhiteSpace();

            var parentFolder = new OsPath(directoryName).Directory.FullPath;
            if (!_diskProvider.FolderExists(parentFolder))
            {
                CreateFolder(parentFolder);
            }

            try
            {
                _diskProvider.CreateFolder(directoryName);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Unable to create directory: {0}", directoryName);
            }

            _mediaFileAttributeService.SetFolderPermissions(directoryName);
        }
    }
}
