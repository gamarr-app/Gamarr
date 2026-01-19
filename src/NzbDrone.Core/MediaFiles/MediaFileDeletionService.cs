using System;
using System.IO;
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
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MediaFileDeletionService(IDiskProvider diskProvider,
                                        IRecycleBinProvider recycleBinProvider,
                                        IMediaFileService mediaFileService,
                                        IGameService gameService,
                                        IConfigService configService,
                                        IEventAggregator eventAggregator,
                                        Logger logger)
        {
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _gameService = gameService;
            _configService = configService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void DeleteGameFile(Game game, GameFile gameFile)
        {
            var fullPath = Path.Combine(game.Path, gameFile.RelativePath);
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

            if (_diskProvider.FolderExists(game.Path) && _diskProvider.FileExists(fullPath))
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
                    throw new NzbDroneClientException(HttpStatusCode.InternalServerError, "Unable to delete game file");
                }
            }

            // Delete the game file from the database to clean it up even if the file was already deleted
            _mediaFileService.Delete(gameFile, DeleteMediaFileReason.Manual);

            _eventAggregator.PublishEvent(new DeleteCompletedEvent());
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
                        _recycleBinProvider.DeleteFolder(game.Path);
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
        }
    }
}
