using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Extras.Files
{
    public interface IExtraFileRepository<TExtraFile> : IBasicRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        void DeleteForGames(List<int> gameIds);
        void DeleteForGameFile(int gameFileId);
        List<TExtraFile> GetFilesByGame(int gameId);
        List<TExtraFile> GetFilesByGameFile(int gameFileId);
        TExtraFile FindByPath(int gameId, string path);
    }

    public class ExtraFileRepository<TExtraFile> : BasicRepository<TExtraFile>, IExtraFileRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        public ExtraFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteForGames(List<int> gameIds)
        {
            Delete(x => gameIds.Contains(x.GameId));
        }

        public void DeleteForGameFile(int gameFileId)
        {
            Delete(x => x.GameFileId == gameFileId);
        }

        public List<TExtraFile> GetFilesByGame(int gameId)
        {
            return Query(x => x.GameId == gameId);
        }

        public List<TExtraFile> GetFilesByGameFile(int gameFileId)
        {
            return Query(x => x.GameFileId == gameFileId);
        }

        public TExtraFile FindByPath(int gameId, string path)
        {
            return Query(c => c.GameId == gameId && c.RelativePath == path).SingleOrDefault();
        }
    }
}
