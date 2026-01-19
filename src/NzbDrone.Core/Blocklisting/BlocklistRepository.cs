using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistRepository : IBasicRepository<Blocklist>
    {
        List<Blocklist> BlocklistedByTitle(int gameId, string sourceTitle);
        List<Blocklist> BlocklistedByTorrentInfoHash(int gameId, string torrentInfoHash);
        List<Blocklist> BlocklistedByGame(int gameId);
        void DeleteForGames(List<int> gameIds);
    }

    public class BlocklistRepository : BasicRepository<Blocklist>, IBlocklistRepository
    {
        public BlocklistRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Blocklist> BlocklistedByTitle(int gameId, string sourceTitle)
        {
            return Query(x => x.GameId == gameId && x.SourceTitle.Contains(sourceTitle));
        }

        public List<Blocklist> BlocklistedByTorrentInfoHash(int gameId, string torrentInfoHash)
        {
            return Query(x => x.GameId == gameId && x.TorrentInfoHash.Contains(torrentInfoHash));
        }

        public List<Blocklist> BlocklistedByGame(int gameId)
        {
            var builder = Builder().Join<Blocklist, Game>((h, a) => h.GameId == a.Id)
                                   .Where<Blocklist>(h => h.GameId == gameId);

            return _database.QueryJoined<Blocklist, Game>(builder, (blocklist, game) =>
            {
                blocklist.Game = game;
                return blocklist;
            }).OrderByDescending(h => h.Date).ToList();
        }

        public void DeleteForGames(List<int> gameIds)
        {
            Delete(x => gameIds.Contains(x.GameId));
        }

        public override PagingSpec<Blocklist> GetPaged(PagingSpec<Blocklist> pagingSpec)
        {
            pagingSpec.Records = GetPagedRecords(PagedBuilder(), pagingSpec, PagedQuery);

            var countTemplate = $"SELECT COUNT(*) FROM (SELECT /**select**/ FROM \"{TableMapping.Mapper.TableNameMapping(typeof(Blocklist))}\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/) AS \"Inner\"";
            pagingSpec.TotalRecords = GetPagedRecordCount(PagedBuilder().Select(typeof(Blocklist)), pagingSpec, countTemplate);

            return pagingSpec;
        }

        protected override SqlBuilder PagedBuilder()
        {
            var builder = Builder()
                .Join<Blocklist, Game>((b, m) => b.GameId == m.Id)
                .LeftJoin<Game, GameMetadata>((m, mm) => m.GameMetadataId == mm.Id);

            return builder;
        }

        protected override IEnumerable<Blocklist> PagedQuery(SqlBuilder builder) =>
            _database.QueryJoined<Blocklist, Game>(builder, (blocklist, game) =>
            {
                blocklist.Game = game;
                return blocklist;
            });
    }
}
