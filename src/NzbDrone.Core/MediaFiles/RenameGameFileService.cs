using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRenameGameFileService
    {
        List<RenameGameFilePreview> GetRenamePreviews(List<int> gameIds);
    }

    public class RenameGameFileService : IRenameGameFileService,
                                          IExecute<RenameFilesCommand>,
                                          IExecute<RenameGameCommand>
    {
        private readonly IGameService _gameService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMoveGameFiles _gameFileMover;
        private readonly IEventAggregator _eventAggregator;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public RenameGameFileService(IGameService gameService,
                                      IMediaFileService mediaFileService,
                                      IMoveGameFiles gameFileMover,
                                      IEventAggregator eventAggregator,
                                      IBuildFileNames filenameBuilder,
                                      IDiskProvider diskProvider,
                                      Logger logger)
        {
            _gameService = gameService;
            _mediaFileService = mediaFileService;
            _gameFileMover = gameFileMover;
            _eventAggregator = eventAggregator;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<RenameGameFilePreview> GetRenamePreviews(List<int> gameIds)
        {
            var games = _gameService.GetGames(gameIds);
            var gameFiles = _mediaFileService.GetFilesByGames(gameIds).ToLookup(f => f.GameId);

            return games.SelectMany(game =>
                {
                    var files = gameFiles[game.Id].ToList();

                    return GetPreviews(game, files);
                })
                .ToList();
        }

        private IEnumerable<RenameGameFilePreview> GetPreviews(Game game, List<GameFile> files)
        {
            foreach (var file in files)
            {
                var gameFilePath = Path.Combine(game.Path, file.RelativePath);

                var newName = _filenameBuilder.BuildFileName(game, file);
                var newPath = _filenameBuilder.BuildFilePath(game, newName, Path.GetExtension(gameFilePath));

                if (!gameFilePath.PathEquals(newPath, StringComparison.Ordinal))
                {
                    yield return new RenameGameFilePreview
                    {
                        GameId = game.Id,
                        GameFileId = file.Id,
                        ExistingPath = file.RelativePath,

                        NewPath = game.Path.GetRelativePath(newPath)
                    };
                }
            }
        }

        private List<RenamedGameFile> RenameFiles(List<GameFile> gameFiles, Game game)
        {
            var renamed = new List<RenamedGameFile>();

            foreach (var gameFile in gameFiles)
            {
                var previousRelativePath = gameFile.RelativePath;
                var previousPath = Path.Combine(game.Path, gameFile.RelativePath);

                try
                {
                    _logger.Debug("Renaming game file: {0}", gameFile);
                    _gameFileMover.MoveGameFile(gameFile, game);

                    _mediaFileService.Update(gameFile);
                    _gameService.UpdateGame(game);
                    renamed.Add(new RenamedGameFile
                                {
                                    GameFile = gameFile,
                                    PreviousRelativePath = previousRelativePath,
                                    PreviousPath = previousPath
                                });

                    _logger.Debug("Renamed game file: {0}", gameFile);

                    _eventAggregator.PublishEvent(new GameFileRenamedEvent(game, gameFile, previousPath));
                }
                catch (FileAlreadyExistsException ex)
                {
                    _logger.Warn("File not renamed, there is already a file at the destination: {0}", ex.Filename);
                }
                catch (SameFilenameException ex)
                {
                    _logger.Debug("File not renamed, source and destination are the same: {0}", ex.Filename);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to rename file: {0}", previousPath);
                }
            }

            if (renamed.Any())
            {
                _diskProvider.RemoveEmptySubfolders(game.Path);

                _eventAggregator.PublishEvent(new GameRenamedEvent(game, renamed));
            }

            return renamed;
        }

        public void Execute(RenameFilesCommand message)
        {
            var game = _gameService.GetGame(message.GameId);
            var gameFiles = _mediaFileService.GetGames(message.Files);

            _logger.ProgressInfo("Renaming {0} files for {1}", gameFiles.Count, game.Title);
            var renamedFiles = RenameFiles(gameFiles, game);
            _logger.ProgressInfo("{0} selected game files renamed for {1}", renamedFiles.Count, game.Title);

            _eventAggregator.PublishEvent(new RenameCompletedEvent());
        }

        public void Execute(RenameGameCommand message)
        {
            _logger.Debug("Renaming game files for selected game");
            var gamesToRename = _gameService.GetGames(message.GameIds);

            foreach (var game in gamesToRename)
            {
                var gameFiles = _mediaFileService.GetFilesByGame(game.Id);
                _logger.ProgressInfo("Renaming game files for {0}", game.Title);
                var renamedFiles = RenameFiles(gameFiles, game);
                _logger.ProgressInfo("{0} game files renamed for {1}", renamedFiles.Count, game.Title);
            }

            _eventAggregator.PublishEvent(new RenameCompletedEvent());
        }
    }
}
