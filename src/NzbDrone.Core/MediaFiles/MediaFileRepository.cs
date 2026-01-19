using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileRepository : IBasicRepository<GameFile>
    {
        List<GameFile> GetFilesByGame(int gameId);
        List<GameFile> GetFilesByGames(IEnumerable<int> gameIds);
        List<GameFile> GetFilesWithoutMediaInfo();
        void DeleteForGames(List<int> gameIds);

        List<GameFile> GetFilesWithRelativePath(int gameId, string relativePath);
    }

    public class MediaFileRepository : BasicRepository<GameFile>, IMediaFileRepository
    {
        public MediaFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<GameFile> GetFilesByGame(int gameId)
        {
            return Query(x => x.GameId == gameId);
        }

        public List<GameFile> GetFilesByGames(IEnumerable<int> gameIds)
        {
            return Query(x => gameIds.Contains(x.GameId));
        }

        public List<GameFile> GetFilesWithoutMediaInfo()
        {
            return Query(x => x.MediaInfo == null);
        }

        public void DeleteForGames(List<int> gameIds)
        {
            Delete(x => gameIds.Contains(x.GameId));
        }

        public List<GameFile> GetFilesWithRelativePath(int gameId, string relativePath)
        {
            return Query(c => c.GameId == gameId && c.RelativePath == relativePath);
        }
    }
}
