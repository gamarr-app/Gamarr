using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.ImportLists.ImportExclusions
{
    public interface IImportListExclusionRepository : IBasicRepository<ImportListExclusion>
    {
        bool IsGameExcluded(int igdbid);
        ImportListExclusion FindByIgdbid(int igdbid);
        List<int> AllExcludedIgdbIds();
    }

    public class ImportListListExclusionRepository : BasicRepository<ImportListExclusion>, IImportListExclusionRepository
    {
        public ImportListListExclusionRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public bool IsGameExcluded(int igdbid)
        {
            return Query(x => x.IgdbId == igdbid).Any();
        }

        public ImportListExclusion FindByIgdbid(int igdbid)
        {
            return Query(x => x.IgdbId == igdbid).SingleOrDefault();
        }

        public List<int> AllExcludedIgdbIds()
        {
            using var conn = _database.OpenConnection();

            return conn.Query<int>("SELECT \"IgdbId\" FROM \"ImportExclusions\"").ToList();
        }
    }
}
