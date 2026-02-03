using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.ImportLists.ImportExclusions
{
    public interface IImportListExclusionRepository : IBasicRepository<ImportListExclusion>
    {
        bool IsGameExcluded(int igdbId, int steamAppId);
        ImportListExclusion FindByIgdbId(int igdbId);
        ImportListExclusion FindBySteamAppId(int steamAppId);
        List<int> AllExcludedIgdbIds();
        List<int> AllExcludedSteamAppIds();
    }

    public class ImportListListExclusionRepository : BasicRepository<ImportListExclusion>, IImportListExclusionRepository
    {
        public ImportListListExclusionRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public bool IsGameExcluded(int igdbId, int steamAppId)
        {
            return Query(x => (igdbId > 0 && x.IgdbId == igdbId) || (steamAppId > 0 && x.SteamAppId == steamAppId)).Any();
        }

        public ImportListExclusion FindByIgdbId(int igdbId)
        {
            return Query(x => x.IgdbId == igdbId).SingleOrDefault();
        }

        public ImportListExclusion FindBySteamAppId(int steamAppId)
        {
            return Query(x => x.SteamAppId == steamAppId).SingleOrDefault();
        }

        public List<int> AllExcludedIgdbIds()
        {
            using var conn = _database.OpenConnection();

            return conn.Query<int>("SELECT \"IgdbId\" FROM \"ImportExclusions\" WHERE \"IgdbId\" > 0").ToList();
        }

        public List<int> AllExcludedSteamAppIds()
        {
            using var conn = _database.OpenConnection();

            return conn.Query<int>("SELECT \"SteamAppId\" FROM \"ImportExclusions\" WHERE \"SteamAppId\" > 0").ToList();
        }
    }
}
