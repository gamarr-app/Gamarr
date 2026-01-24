using System;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Games.Collections
{
    public interface IAddGameCollectionService
    {
        GameCollection AddGameCollection(GameCollection newCollection);
    }

    public class AddGameCollectionService : IAddGameCollectionService
    {
        private readonly IGameCollectionService _collectionService;
        private readonly IProvideGameInfo _gameInfo;
        private readonly Logger _logger;

        public AddGameCollectionService(IGameCollectionService collectionService,
                                IProvideGameInfo gameInfo,
                                Logger logger)
        {
            _collectionService = collectionService;
            _gameInfo = gameInfo;
            _logger = logger;
        }

        public GameCollection AddGameCollection(GameCollection newCollection)
        {
            Ensure.That(newCollection, () => newCollection).IsNotNull();

            var existingCollection = _collectionService.FindByIgdbId(newCollection.IgdbId);

            if (existingCollection != null)
            {
                return existingCollection;
            }

            newCollection = AddSkyhookData(newCollection);

            if (newCollection == null)
            {
                return null;
            }

            newCollection = SetPropertiesAndValidate(newCollection);

            _logger.Info("Adding Collection {0}[{1}]", newCollection.Title, newCollection.IgdbId);

            _collectionService.AddCollection(newCollection);

            return newCollection;
        }

        private GameCollection AddSkyhookData(GameCollection newCollection)
        {
            GameCollection collection;

            try
            {
                collection = _gameInfo.GetCollectionInfo(newCollection.IgdbId);
            }
            catch (GameNotFoundException)
            {
                _logger.Error("IgdbId {0} was not found, it may have been removed from IGDB.", newCollection.IgdbId);

                return null;
            }

            if (collection == null)
            {
                _logger.Warn("Could not retrieve collection info for IgdbId {0}", newCollection.IgdbId);
                return null;
            }

            collection.ApplyChanges(newCollection);

            return collection;
        }

        private GameCollection SetPropertiesAndValidate(GameCollection newCollection)
        {
            newCollection.CleanTitle = newCollection.Title.CleanGameTitle();
            newCollection.SortTitle = GameTitleNormalizer.Normalize(newCollection.Title, newCollection.IgdbId);
            newCollection.Added = DateTime.UtcNow;

            return newCollection;
        }
    }
}
