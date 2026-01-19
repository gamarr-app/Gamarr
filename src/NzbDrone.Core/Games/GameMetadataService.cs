using System.Collections.Generic;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.Games
{
    public interface IGameMetadataService
    {
        GameMetadata Get(int id);
        GameMetadata FindByIgdbId(int igdbId);
        List<GameMetadata> GetGamesWithCollections();
        List<GameMetadata> GetGamesByCollectionIgdbId(int collectionId);
        bool Upsert(GameMetadata game);
        bool UpsertMany(List<GameMetadata> games);
        void DeleteMany(List<GameMetadata> games);
    }

    public class GameMetadataService : IGameMetadataService
    {
        private readonly IGameMetadataRepository _gameMetadataRepository;
        private readonly IGameService _gameService;
        private readonly IImportListGameService _importListGameService;

        public GameMetadataService(IGameMetadataRepository gameMetadataRepository, IGameService gameService, IImportListGameService importListGameService)
        {
            _gameMetadataRepository = gameMetadataRepository;
            _gameService = gameService;
            _importListGameService = importListGameService;
        }

        public GameMetadata FindByIgdbId(int igdbId)
        {
            return _gameMetadataRepository.FindByIgdbId(igdbId);
        }

        public List<GameMetadata> GetGamesWithCollections()
        {
            return _gameMetadataRepository.GetGamesWithCollections();
        }

        public List<GameMetadata> GetGamesByCollectionIgdbId(int collectionId)
        {
            return _gameMetadataRepository.GetGamesByCollectionIgdbId(collectionId);
        }

        public GameMetadata Get(int id)
        {
            return _gameMetadataRepository.Get(id);
        }

        public bool Upsert(GameMetadata game)
        {
            return _gameMetadataRepository.UpsertMany(new List<GameMetadata> { game });
        }

        public bool UpsertMany(List<GameMetadata> games)
        {
            return _gameMetadataRepository.UpsertMany(games);
        }

        public void DeleteMany(List<GameMetadata> games)
        {
            foreach (var game in games)
            {
                if (!_importListGameService.ExistsByMetadataId(game.Id) && !_gameService.ExistsByMetadataId(game.Id))
                {
                    _gameMetadataRepository.Delete(game);
                }
            }
        }
    }
}
