using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.ImportLists.ImportListGames
{
    public interface IImportListGameService
    {
        List<ImportListGame> GetAllListGames();
        List<ImportListGame> GetAllForLists(List<int> listIds);
        ImportListGame AddListGame(ImportListGame listGame);
        List<ImportListGame> AddListGames(List<ImportListGame> listGames);
        List<ImportListGame> SyncGamesForList(List<ImportListGame> listGames, int listId);
        void RemoveListGame(ImportListGame listGame);
        ImportListGame GetById(int id);
        bool ExistsByMetadataId(int metadataId);
    }

    public class ImportListGameService : IImportListGameService, IHandleAsync<ProviderDeletedEvent<IImportList>>
    {
        private readonly IImportListGameRepository _importListGameRepository;
        private readonly Logger _logger;

        public ImportListGameService(IImportListGameRepository importListGameRepository,
                             Logger logger)
        {
            _importListGameRepository = importListGameRepository;
            _logger = logger;
        }

        public ImportListGame AddListGame(ImportListGame exclusion)
        {
            return _importListGameRepository.Insert(exclusion);
        }

        public List<ImportListGame> AddListGames(List<ImportListGame> listGames)
        {
            _importListGameRepository.InsertMany(listGames);

            return listGames;
        }

        public List<ImportListGame> SyncGamesForList(List<ImportListGame> listGames, int listId)
        {
            var existingListGames = GetAllForLists(new List<int> { listId });

            // Match by IgdbId or SteamAppId
            listGames.ForEach(l => l.Id = existingListGames.FirstOrDefault(e =>
                (l.IgdbId > 0 && e.IgdbId == l.IgdbId) ||
                (l.SteamAppId > 0 && e.SteamAppId == l.SteamAppId))?.Id ?? 0);

            _importListGameRepository.InsertMany(listGames.Where(l => l.Id == 0).ToList());
            _importListGameRepository.UpdateMany(listGames.Where(l => l.Id > 0).ToList());
            _importListGameRepository.DeleteMany(existingListGames.Where(l => listGames.All(x =>
                (l.IgdbId == 0 || x.IgdbId != l.IgdbId) &&
                (l.SteamAppId == 0 || x.SteamAppId != l.SteamAppId))).ToList());

            return listGames;
        }

        public List<ImportListGame> GetAllListGames()
        {
            return _importListGameRepository.All().ToList();
        }

        public List<ImportListGame> GetAllForLists(List<int> listIds)
        {
            return _importListGameRepository.GetAllForLists(listIds).ToList();
        }

        public void RemoveListGame(ImportListGame listGame)
        {
            _importListGameRepository.Delete(listGame);
        }

        public ImportListGame GetById(int id)
        {
            return _importListGameRepository.Get(id);
        }

        public void HandleAsync(ProviderDeletedEvent<IImportList> message)
        {
            var gamesOnList = _importListGameRepository.GetAllForLists(new List<int> { message.ProviderId });
            _importListGameRepository.DeleteMany(gamesOnList);
        }

        public bool ExistsByMetadataId(int metadataId)
        {
            return _importListGameRepository.ExistsByMetadataId(metadataId);
        }
    }
}
