using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games.Translations
{
    [TestFixture]
    public class GameTranslationServiceFixture : CoreTest<GameTranslationService>
    {
        private GameMetadata _gameMetadata;

        [SetUp]
        public void Setup()
        {
            _gameMetadata = new GameMetadata
            {
                Id = 1,
                IgdbId = 100,
                Title = "Test Game"
            };

            Mocker.GetMock<IGameTranslationRepository>()
                  .Setup(s => s.FindByGameMetadataId(_gameMetadata.Id))
                  .Returns(new List<GameTranslation>());
        }

        private GameTranslation CreateTranslation(int id, Language language, string title)
        {
            return new GameTranslation
            {
                Id = id,
                GameMetadataId = _gameMetadata.Id,
                Language = language,
                Title = title,
                CleanTitle = title.ToLower(),
                Overview = "Overview in " + language.Name
            };
        }

        [Test]
        public void should_add_new_translations()
        {
            var newTranslations = new List<GameTranslation>
            {
                CreateTranslation(0, Language.French, "Jeu Test"),
                CreateTranslation(0, Language.German, "Testspiel")
            };

            Subject.UpdateTranslations(newTranslations, _gameMetadata);

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.InsertMany(It.Is<List<GameTranslation>>(l => l.Count == 2)), Times.Once());

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.UpdateMany(It.Is<List<GameTranslation>>(l => l.Count == 0)), Times.Once());

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.DeleteMany(It.Is<List<GameTranslation>>(l => l.Count == 0)), Times.Once());
        }

        [Test]
        public void should_update_existing_translations()
        {
            var existingTranslations = new List<GameTranslation>
            {
                CreateTranslation(1, Language.French, "Ancien Titre")
            };

            Mocker.GetMock<IGameTranslationRepository>()
                  .Setup(s => s.FindByGameMetadataId(_gameMetadata.Id))
                  .Returns(existingTranslations);

            var updatedTranslations = new List<GameTranslation>
            {
                CreateTranslation(0, Language.French, "Nouveau Titre")
            };

            Subject.UpdateTranslations(updatedTranslations, _gameMetadata);

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.UpdateMany(It.Is<List<GameTranslation>>(l => l.Count == 1 && l[0].Title == "Nouveau Titre")), Times.Once());

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.InsertMany(It.Is<List<GameTranslation>>(l => l.Count == 0)), Times.Once());

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.DeleteMany(It.Is<List<GameTranslation>>(l => l.Count == 0)), Times.Once());
        }

        [Test]
        public void should_delete_removed_translations()
        {
            var existingTranslations = new List<GameTranslation>
            {
                CreateTranslation(1, Language.French, "Jeu Test"),
                CreateTranslation(2, Language.German, "Testspiel")
            };

            Mocker.GetMock<IGameTranslationRepository>()
                  .Setup(s => s.FindByGameMetadataId(_gameMetadata.Id))
                  .Returns(existingTranslations);

            var updatedTranslations = new List<GameTranslation>
            {
                CreateTranslation(0, Language.French, "Jeu Test")
            };

            Subject.UpdateTranslations(updatedTranslations, _gameMetadata);

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.DeleteMany(It.Is<List<GameTranslation>>(l => l.Count == 1 && l[0].Language == Language.German)), Times.Once());
        }

        [Test]
        public void should_deduplicate_by_language()
        {
            var duplicateTranslations = new List<GameTranslation>
            {
                CreateTranslation(0, Language.French, "Titre Un"),
                CreateTranslation(0, Language.French, "Titre Deux"),
                CreateTranslation(0, Language.German, "Testspiel")
            };

            Subject.UpdateTranslations(duplicateTranslations, _gameMetadata);

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.InsertMany(It.Is<List<GameTranslation>>(l => l.Count == 2)), Times.Once());
        }

        [Test]
        public void should_filter_out_translations_with_null_language()
        {
            var translations = new List<GameTranslation>
            {
                CreateTranslation(0, Language.French, "Jeu Test"),
                new GameTranslation { Title = "No Language", Language = null }
            };

            Subject.UpdateTranslations(translations, _gameMetadata);

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.InsertMany(It.Is<List<GameTranslation>>(l => l.Count == 1 && l[0].Language == Language.French)), Times.Once());
        }

        [Test]
        public void should_set_game_metadata_id_on_all_translations()
        {
            var translations = new List<GameTranslation>
            {
                new GameTranslation { Language = Language.French, Title = "Titre", GameMetadataId = 999 }
            };

            var result = Subject.UpdateTranslations(translations, _gameMetadata);

            result.Should().OnlyContain(t => t.GameMetadataId == _gameMetadata.Id);
        }

        [Test]
        public void should_not_update_translation_that_has_not_changed()
        {
            var existingTranslation = CreateTranslation(1, Language.French, "Jeu Test");

            Mocker.GetMock<IGameTranslationRepository>()
                  .Setup(s => s.FindByGameMetadataId(_gameMetadata.Id))
                  .Returns(new List<GameTranslation> { existingTranslation });

            var sameTranslation = CreateTranslation(0, Language.French, "Jeu Test");

            Subject.UpdateTranslations(new List<GameTranslation> { sameTranslation }, _gameMetadata);

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.UpdateMany(It.Is<List<GameTranslation>>(l => l.Count == 0)), Times.Once());

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.InsertMany(It.Is<List<GameTranslation>>(l => l.Count == 0)), Times.Once());

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.DeleteMany(It.Is<List<GameTranslation>>(l => l.Count == 0)), Times.Once());
        }

        [Test]
        public void should_delete_translations_on_game_deletion()
        {
            var games = new List<Game>
            {
                new Game { GameMetadataId = 1 },
                new Game { GameMetadataId = 2 }
            };

            var deleteEvent = new GamesDeletedEvent(games, false, false);

            Subject.HandleAsync(deleteEvent);

            Mocker.GetMock<IGameTranslationRepository>()
                  .Verify(v => v.DeleteForGames(It.Is<List<int>>(l => l.Count == 2 && l.Contains(1) && l.Contains(2))), Times.Once());
        }
    }
}
