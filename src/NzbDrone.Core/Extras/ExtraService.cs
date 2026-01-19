using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Extras
{
    public interface IExtraService
    {
        void MoveFilesAfterRename(Game game, GameFile gameFile);
        void ImportGame(LocalGame localGame, GameFile gameFile, bool isReadOnly);
    }

    public class ExtraService : IExtraService,
                                IHandle<MediaCoversUpdatedEvent>,
                                IHandle<GameFolderCreatedEvent>,
                                IHandle<GameScannedEvent>,
                                IHandle<GameRenamedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IGameService _gameService;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly List<IManageExtraFiles> _extraFileManagers;

        public ExtraService(IMediaFileService mediaFileService,
                            IGameService gameService,
                            IDiskProvider diskProvider,
                            IConfigService configService,
                            IEnumerable<IManageExtraFiles> extraFileManagers,
                            Logger logger)
        {
            _mediaFileService = mediaFileService;
            _gameService = gameService;
            _diskProvider = diskProvider;
            _configService = configService;
            _extraFileManagers = extraFileManagers.OrderBy(e => e.Order).ToList();
        }

        public void ImportGame(LocalGame localGame, GameFile gameFile, bool isReadOnly)
        {
            ImportExtraFiles(localGame, gameFile, isReadOnly);

            CreateAfterGameImport(localGame.Game, gameFile);
        }

        public void ImportExtraFiles(LocalGame localGame, GameFile gameFile, bool isReadOnly)
        {
            if (!_configService.ImportExtraFiles)
            {
                return;
            }

            var folderSearchOption = localGame.FolderGameInfo != null;

            var wantedExtensions = _configService.ExtraFileExtensions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                     .Select(e => e.Trim(' ', '.')
                                                                     .Insert(0, "."))
                                                                     .ToList();

            var sourceFolder = _diskProvider.GetParentFolder(localGame.Path);
            var files = _diskProvider.GetFiles(sourceFolder, folderSearchOption);
            var managedFiles = _extraFileManagers.Select((i) => new List<string>()).ToArray();

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                var matchingExtension = wantedExtensions.FirstOrDefault(e => e.Equals(extension));

                if (matchingExtension == null)
                {
                    continue;
                }

                for (var i = 0; i < _extraFileManagers.Count; i++)
                {
                    if (_extraFileManagers[i].CanImportFile(localGame, gameFile, file, extension, isReadOnly))
                    {
                        managedFiles[i].Add(file);
                        break;
                    }
                }
            }

            for (var i = 0; i < _extraFileManagers.Count; i++)
            {
                _extraFileManagers[i].ImportFiles(localGame, gameFile, managedFiles[i], isReadOnly);
            }
        }

        private void CreateAfterGameImport(Game game, GameFile gameFile)
        {
            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterGameImport(game, gameFile);
            }
        }

        public void Handle(MediaCoversUpdatedEvent message)
        {
            if (message.Updated)
            {
                var game = message.Game;

                foreach (var extraFileManager in _extraFileManagers)
                {
                    extraFileManager.CreateAfterMediaCoverUpdate(game);
                }
            }
        }

        public void Handle(GameScannedEvent message)
        {
            var game = message.Game;
            var gameFiles = GetGameFiles(game.Id);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterGameScan(game, gameFiles);
            }
        }

        public void Handle(GameFolderCreatedEvent message)
        {
            var game = message.Game;

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterGameFolder(game, message.GameFolder);
            }
        }

        public void MoveFilesAfterRename(Game game, GameFile gameFile)
        {
            var gameFiles = new List<GameFile> { gameFile };

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.MoveFilesAfterRename(game, gameFiles);
            }
        }

        public void Handle(GameRenamedEvent message)
        {
            var game = message.Game;
            var gameFiles = GetGameFiles(game.Id);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.MoveFilesAfterRename(game, gameFiles);
            }
        }

        private List<GameFile> GetGameFiles(int gameId)
        {
            var gameFiles = _mediaFileService.GetFilesByGame(gameId);

            foreach (var gameFile in gameFiles)
            {
                gameFile.Game = _gameService.GetGame(gameId);
            }

            return gameFiles;
        }
    }
}
