using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Games
{
    public class MoveGameService : IExecute<MoveGameCommand>, IExecute<BulkMoveGameCommand>
    {
        private readonly IGameService _gameService;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MoveGameService(IGameService gameService,
                                 IBuildFileNames filenameBuilder,
                                 IDiskProvider diskProvider,
                                 IDiskTransferService diskTransferService,
                                 IEventAggregator eventAggregator,
                                 Logger logger)
        {
            _gameService = gameService;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _diskTransferService = diskTransferService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        private void MoveSingleGame(Game game, string sourcePath, string destinationPath, int? index = null, int? total = null)
        {
            if (!sourcePath.IsPathValid(PathValidationType.CurrentOs))
            {
                _logger.Warn("Folder '{0}' for '{1}' is invalid, unable to move game. Try moving files manually", sourcePath, game.Title);
                return;
            }

            if (!_diskProvider.FolderExists(sourcePath))
            {
                _logger.Debug("Folder '{0}' for '{1}' does not exist, not moving.", sourcePath, game.Title);
                return;
            }

            if (index != null && total != null)
            {
                _logger.ProgressInfo("Moving {0} from '{1}' to '{2}' ({3}/{4})", game.Title, sourcePath, destinationPath, index + 1, total);
            }
            else
            {
                _logger.ProgressInfo("Moving {0} from '{1}' to '{2}'", game.Title, sourcePath, destinationPath);
            }

            if (sourcePath.PathEquals(destinationPath))
            {
                _logger.ProgressInfo("{0} is already in the specified location '{1}'.", game, destinationPath);
                return;
            }

            try
            {
                _diskProvider.CreateFolder(new DirectoryInfo(destinationPath).Parent.FullName);
                _diskTransferService.TransferFolder(sourcePath, destinationPath, TransferMode.Move);

                _logger.ProgressInfo("{0} moved successfully to {1}", game.Title, destinationPath);

                _eventAggregator.PublishEvent(new GameMovedEvent(game, sourcePath, destinationPath));
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Unable to move game from '{0}' to '{1}'. Try moving files manually", sourcePath, destinationPath);

                RevertPath(game.Id, sourcePath);
            }
        }

        private void RevertPath(int gameId, string path)
        {
            var game = _gameService.GetGame(gameId);

            game.Path = path;
            _gameService.UpdateGame(game);
        }

        public void Execute(MoveGameCommand message)
        {
            var game = _gameService.GetGame(message.GameId);
            MoveSingleGame(game, message.SourcePath, message.DestinationPath);
        }

        public void Execute(BulkMoveGameCommand message)
        {
            var gamesToMove = message.Games;
            var destinationRootFolder = message.DestinationRootFolder;

            _logger.ProgressInfo("Moving {0} games to '{1}'", gamesToMove.Count, destinationRootFolder);

            for (var index = 0; index < gamesToMove.Count; index++)
            {
                var s = gamesToMove[index];
                var game = _gameService.GetGame(s.GameId);
                var destinationPath = Path.Combine(destinationRootFolder, _filenameBuilder.GetGameFolder(game));

                MoveSingleGame(game, s.SourcePath, destinationPath, index, gamesToMove.Count);
            }

            _logger.ProgressInfo("Finished moving {0} games to '{1}'", gamesToMove.Count, destinationRootFolder);
        }
    }
}
