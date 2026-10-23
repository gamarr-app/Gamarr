using System;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDeleteMediaFiles
    {
        void DeleteGameFile(Game game, GameFile gameFile);
    }

    public class MediaFileDeletionService : IDeleteMediaFiles,
                                            IHandleAsync<GamesDeletedEvent>,
                                            IHandle<GameFileDeletedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IGameService _gameService;
        private readonly IGameComponentRepository _componentRepository;
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MediaFileDeletionService(IDiskProvider diskProvider,
                                        IRecycleBinProvider recycleBinProvider,
                                        IMediaFileService mediaFileService,
                                        IGameService gameService,
                                        IGameComponentRepository componentRepository,
                                        IConfigService configService,
                                        IEventAggregator eventAggregator,
                                        Logger logger)
        {
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _gameService = gameService;
            _componentRepository = componentRepository;
            _configService = configService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void DeleteGameFile(Game game, GameFile gameFile)
        {
            var rootFolder = _diskProvider.GetParentFolder(game.Path);

            if (!_diskProvider.FolderExists(rootFolder))
            {
                _logger.Warn("Game's root folder ({0}) doesn't exist.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Game's root folder ({0}) doesn't exist.", rootFolder);
            }

            if (_diskProvider.GetDirectories(rootFolder).Empty())
            {
                _logger.Warn("Game's root folder ({0}) is empty. Rescan will not update games as a failsafe.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Game's root folder ({0}) is empty. Rescan will not update games as a failsafe.", rootFolder);
            }

            // Handle folder-based GameFiles (the game folder itself IS the file)
            if (gameFile.IsFolder(game))
            {
                EnsureNoComponentsInside(game, gameFile);

                if (_diskProvider.FolderExists(game.Path) &&
                    _diskProvider.GetFiles(game.Path, true).Any())
                {
                    _logger.Info("Deleting game folder contents: {0}", game.Path);

                    try
                    {
                        _recycleBinProvider.DeleteFolder(game.Path);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Unable to delete game folder");
                        throw new NzbDroneClientException(HttpStatusCode.Conflict, "Unable to delete game folder: {0}", e.Message);
                    }
                }
                else
                {
                    _logger.Warn("Game folder is empty or missing, removing the database entry only: {0}", game.Path);
                }
            }
            else
            {
                var fullPath = gameFile.GetPath(game);

                if (!_diskProvider.FolderExists(game.Path))
                {
                    _logger.Warn("Game folder doesn't exist, removing the database entry only: {0}", game.Path);
                }
                else if (_diskProvider.FileExists(fullPath))
                {
                    _logger.Info("Deleting game file: {0}", fullPath);

                    var subfolder = _diskProvider.GetParentFolder(game.Path).GetRelativePath(_diskProvider.GetParentFolder(fullPath));

                    try
                    {
                        _recycleBinProvider.DeleteFile(fullPath, subfolder);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Unable to delete game file");
                        throw new NzbDroneClientException(HttpStatusCode.Conflict, "Unable to delete game file: {0}", e.Message);
                    }
                }
                else if (_diskProvider.FolderExists(fullPath))
                {
                    // An update or DLC imported as a folder (Updates/<version>,
                    // DLC/<name>) has a non-empty RelativePath, so it is not the
                    // game folder, yet its path is a directory. FileExists is
                    // false for a directory, so this used to delete nothing at
                    // all while still dropping the row — the files stayed and
                    // the next rescan re-adopted them (gamarr-app/Gamarr#215).
                    _logger.Info("Deleting game folder: {0}", fullPath);

                    try
                    {
                        _recycleBinProvider.DeleteFolder(fullPath);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Unable to delete game folder");
                        throw new NzbDroneClientException(HttpStatusCode.Conflict, "Unable to delete game folder: {0}", e.Message);
                    }
                }
                else
                {
                    _logger.Warn("Game file no longer exists on disk, removing the database entry only: {0}", fullPath);
                }
            }

            // Delete the game file from the database to clean it up even if the file was already deleted
            _mediaFileService.Delete(gameFile, DeleteMediaFileReason.Manual);

            CleanupComponentSlot(game, gameFile);

            _eventAggregator.PublishEvent(new DeleteCompletedEvent());
        }

        /// <summary>
        /// Refuses to delete the base record while update/DLC folders live inside
        /// the game folder.
        /// </summary>
        /// <remarks>
        /// The base record's path IS the game folder, so recycling it took every
        /// component's Updates/ and DLC/ subfolder with it — records the user
        /// never asked to delete (gamarr-app/Gamarr#215). Nothing on disk says
        /// which loose files belong to the base rather than to a component, so
        /// refusing is the only answer that cannot destroy data; deleting the
        /// components first, or deleting the whole game, both still work.
        /// </remarks>
        private void EnsureNoComponentsInside(Game game, GameFile gameFile)
        {
            var components = _mediaFileService.GetFilesByGame(game.Id)
                                              .Where(f => f.Id != gameFile.Id && f.IsSubfolderUnit(game))
                                              .ToList();

            if (components.Empty())
            {
                return;
            }

            var names = string.Join(", ", components.Select(f => f.RelativePath).Take(5));

            _logger.Warn("Not deleting game folder ({0}): {1} update/DLC file(s) are stored inside it ({2}).", game.Path, components.Count, names);

            throw new NzbDroneClientException(HttpStatusCode.Conflict,
                "Game folder ({0}) also holds {1} update/DLC file(s) ({2}). Delete those first, or delete the game with its files.",
                game.Path,
                components.Count,
                names);
        }

        /// <summary>
        /// Drops the component slot of a deleted update or imported DLC folder.
        /// </summary>
        /// <remarks>
        /// Update slots are keyed by the version folder and imported DLC slots by
        /// the release folder, so once the folder is gone the slot describes
        /// nothing — and being monitored it would be searched for and grabbed
        /// again. Metadata DLC slots (igdb:/steam:) are kept: they are the
        /// catalogue entry, which correctly goes back to wanted. Mirrors
        /// UpdateRetentionService.
        /// </remarks>
        private void CleanupComponentSlot(Game game, GameFile gameFile)
        {
            if (gameFile.ComponentId <= 0 || !gameFile.IsSubfolderUnit(game))
            {
                return;
            }

            var slot = _componentRepository.GetByGame(game.Id)
                                           .FirstOrDefault(c => c.Id == gameFile.ComponentId);

            if (slot == null)
            {
                return;
            }

            var fileDerived = slot.ComponentType == GameComponentType.Update ||
                              (slot.ComponentType == GameComponentType.Dlc && slot.Key.StartsWith("import:", StringComparison.OrdinalIgnoreCase));

            if (!fileDerived)
            {
                return;
            }

            if (_mediaFileService.GetFilesByGame(game.Id).Any(f => f.ComponentId == slot.Id))
            {
                return;
            }

            _logger.Debug("Removing component slot '{0}' left behind by the deleted {1}", slot.Title, gameFile.RelativePath);
            _componentRepository.Delete(slot.Id);
        }

        public void HandleAsync(GamesDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                var allGames = _gameService.AllGamePaths();

                foreach (var game in message.Games)
                {
                    foreach (var s in allGames)
                    {
                        if (s.Key == game.Id)
                        {
                            continue;
                        }

                        if (game.Path.IsParentPath(s.Value))
                        {
                            _logger.Error("Game path: '{0}' is a parent of another game, not deleting files.", game.Path);
                            return;
                        }

                        if (game.Path.PathEquals(s.Value))
                        {
                            _logger.Error("Game path: '{0}' is the same as another game, not deleting files.", game.Path);
                            return;
                        }
                    }

                    if (_diskProvider.FolderExists(game.Path))
                    {
                        try
                        {
                            _recycleBinProvider.DeleteFolder(game.Path);
                        }
                        catch (Exception e)
                        {
                            // Don't let one game's failure abort the rest of the batch or prevent DeleteCompletedEvent,
                            // this runs on the event aggregator thread and there is no caller to report the error to.
                            _logger.Error(e, "Unable to delete game folder: '{0}'", game.Path);
                        }
                    }
                }

                _eventAggregator.PublishEvent(new DeleteCompletedEvent());
            }
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(GameFileDeletedEvent message)
        {
            if (_configService.DeleteEmptyFolders)
            {
                var game = message.GameFile.Game;
                var gamePath = game.Path;
                var folder = message.GameFile.Path.GetParentPath();

                try
                {
                    while (gamePath.IsParentPath(folder))
                    {
                        if (_diskProvider.FolderExists(folder))
                        {
                            _diskProvider.RemoveEmptySubfolders(folder);
                        }

                        folder = folder.GetParentPath();
                    }

                    _diskProvider.RemoveEmptySubfolders(gamePath);

                    if (_diskProvider.FolderEmpty(gamePath))
                    {
                        _diskProvider.DeleteFolder(gamePath, true);
                    }
                }
                catch (Exception e)
                {
                    // Best effort cleanup on the event aggregator thread, an unhandled error here would
                    // only surface as a lost task.
                    _logger.Error(e, "Unable to remove empty folders for game path: '{0}'", gamePath);
                }
            }
        }
    }
}
