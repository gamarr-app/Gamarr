using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileService
    {
        GameFile Add(GameFile gameFile);
        void Update(GameFile gameFile);
        void Update(List<GameFile> gameFile);
        void Delete(GameFile gameFile, DeleteMediaFileReason reason);
        List<GameFile> GetFilesByGame(int gameId);
        List<GameFile> GetFilesByGames(IEnumerable<int> gameIds);
        List<GameFile> GetFilesWithoutMediaInfo();
        List<string> FilterExistingFiles(List<string> files, Game game);
        GameFile GetGame(int id);
        List<GameFile> GetGames(IEnumerable<int> ids);
        List<GameFile> GetFilesWithRelativePath(int gameIds, string relativePath);
    }

    public class MediaFileService : IMediaFileService, IHandleAsync<GamesDeletedEvent>
    {
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IEventAggregator _eventAggregator;

        public MediaFileService(IMediaFileRepository mediaFileRepository,
                                IGameRepository gameRepository,
                                IEventAggregator eventAggregator)
        {
            _mediaFileRepository = mediaFileRepository;
            _gameRepository = gameRepository;
            _eventAggregator = eventAggregator;
        }

        public GameFile Add(GameFile gameFile)
        {
            var addedFile = _mediaFileRepository.Insert(gameFile);
            if (addedFile.Game == null)
            {
                addedFile.Game = _gameRepository.Get(gameFile.GameId);
            }

            _eventAggregator.PublishEvent(new GameFileAddedEvent(addedFile));

            return addedFile;
        }

        public void Update(GameFile gameFile)
        {
            _mediaFileRepository.Update(gameFile);
        }

        public void Update(List<GameFile> gameFiles)
        {
            _mediaFileRepository.UpdateMany(gameFiles);
        }

        public void Delete(GameFile gameFile, DeleteMediaFileReason reason)
        {
            // Little hack so we have the game attached for the event consumers
            if (gameFile.Game == null)
            {
                gameFile.Game = _gameRepository.Get(gameFile.GameId);
            }

            gameFile.Path = gameFile.GetPath();

            _mediaFileRepository.Delete(gameFile);
            _eventAggregator.PublishEvent(new GameFileDeletedEvent(gameFile, reason));
        }

        public List<GameFile> GetFilesByGame(int gameId)
        {
            return _mediaFileRepository.GetFilesByGame(gameId);
        }

        public List<GameFile> GetFilesByGames(IEnumerable<int> gameIds)
        {
            return _mediaFileRepository.GetFilesByGames(gameIds);
        }

        public List<GameFile> GetFilesWithoutMediaInfo()
        {
            return _mediaFileRepository.GetFilesWithoutMediaInfo();
        }

        public List<string> FilterExistingFiles(List<string> files, Game game)
        {
            var gameFiles = GetFilesByGame(game.Id).Select(f => f.GetPath(game)).ToList();

            if (!gameFiles.Any())
            {
                return files;
            }

            return files.Except(gameFiles, PathEqualityComparer.Instance).ToList();
        }

        public List<GameFile> GetGames(IEnumerable<int> ids)
        {
            return _mediaFileRepository.Get(ids).ToList();
        }

        public GameFile GetGame(int id)
        {
            return _mediaFileRepository.Get(id);
        }

        public List<GameFile> GetFilesWithRelativePath(int gameId, string relativePath)
        {
            return _mediaFileRepository.GetFilesWithRelativePath(gameId, relativePath);
        }

        public void HandleAsync(GamesDeletedEvent message)
        {
            _mediaFileRepository.DeleteForGames(message.Games.Select(m => m.Id).ToList());
        }

        public static List<string> FilterExistingFiles(List<string> files, List<GameFile> gameFiles, Game game)
        {
            var seriesFilePaths = gameFiles.Select(f => f.GetPath(game)).ToList();

            if (!seriesFilePaths.Any())
            {
                return files;
            }

            return files.Except(seriesFilePaths, PathEqualityComparer.Instance).ToList();
        }
    }
}
