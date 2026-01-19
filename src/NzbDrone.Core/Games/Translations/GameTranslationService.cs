using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.Games.Translations
{
    public interface IGameTranslationService
    {
        List<GameTranslation> GetAllTranslationsForGameMetadata(int gameMetadataId);
        List<GameTranslation> GetAllTranslationsForLanguage(Language language);
        List<GameTranslation> UpdateTranslations(List<GameTranslation> titles, GameMetadata game);
    }

    public class GameTranslationService : IGameTranslationService, IHandleAsync<GamesDeletedEvent>
    {
        private readonly IGameTranslationRepository _translationRepo;
        private readonly Logger _logger;

        public GameTranslationService(IGameTranslationRepository translationRepo,
                             Logger logger)
        {
            _translationRepo = translationRepo;
            _logger = logger;
        }

        public List<GameTranslation> GetAllTranslationsForGameMetadata(int gameMetadataId)
        {
            return _translationRepo.FindByGameMetadataId(gameMetadataId).ToList();
        }

        public List<GameTranslation> GetAllTranslationsForLanguage(Language language)
        {
            return _translationRepo.FindByLanguage(language).ToList();
        }

        public void RemoveTitle(GameTranslation title)
        {
            _translationRepo.Delete(title);
        }

        public List<GameTranslation> UpdateTranslations(List<GameTranslation> translations, GameMetadata gameMetadata)
        {
            var gameMetadataId = gameMetadata.Id;

            // First update the game ids so we can correlate them later
            translations.ForEach(t => t.GameMetadataId = gameMetadataId);

            // Then throw out any we don't have languages for
            translations = translations.Where(t => t.Language != null).ToList();

            // Then make sure they are all distinct languages
            translations = translations.DistinctBy(t => t.Language).ToList();

            // Now find translations to delete, update and insert
            var existingTranslations = _translationRepo.FindByGameMetadataId(gameMetadataId);

            var updateList = new List<GameTranslation>();
            var addList = new List<GameTranslation>();
            var upToDateCount = 0;

            foreach (var translation in translations)
            {
                var existingTranslation = existingTranslations.FirstOrDefault(x => x.Language == translation.Language);

                if (existingTranslation != null)
                {
                    existingTranslations.Remove(existingTranslation);

                    translation.UseDbFieldsFrom(existingTranslation);

                    if (!translation.Equals(existingTranslation))
                    {
                        updateList.Add(translation);
                    }
                    else
                    {
                        upToDateCount++;
                    }
                }
                else
                {
                    addList.Add(translation);
                }
            }

            _translationRepo.DeleteMany(existingTranslations);
            _translationRepo.UpdateMany(updateList);
            _translationRepo.InsertMany(addList);

            _logger.Debug("[{0}] {1} translations up to date; Updating {2}, Adding {3}, Deleting {4} entries.", gameMetadata.Title, upToDateCount, updateList.Count, addList.Count, existingTranslations.Count);

            return translations;
        }

        public void HandleAsync(GamesDeletedEvent message)
        {
            // TODO handle metadata delete instead of game delete
            _translationRepo.DeleteForGames(message.Games.Select(m => m.GameMetadataId).ToList());
        }
    }
}
