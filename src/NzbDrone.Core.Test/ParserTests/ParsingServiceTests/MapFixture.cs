using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class MapFixture : TestBase<ParsingService>
    {
        private Game _game;
        private ParsedGameInfo _parsedGameInfo;
        private ParsedGameInfo _wrongYearInfo;
        private ParsedGameInfo _wrongTitleInfo;
        private ParsedGameInfo _romanTitleInfo;
        private ParsedGameInfo _alternativeTitleInfo;
        private ParsedGameInfo _translationTitleInfo;
        private ParsedGameInfo _umlautInfo;
        private ParsedGameInfo _umlautAltInfo;
        private ParsedGameInfo _multiLanguageInfo;
        private ParsedGameInfo _multiLanguageWithOriginalInfo;
        private GameSearchCriteria _gameSearchCriteria;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                   .With(m => m.Title = "Fack Ju Göthe 2")
                                   .With(m => m.GameMetadata.Value.CleanTitle = "fackjugoethe2")
                                   .With(m => m.Year = 2015)
                                   .With(m => m.GameMetadata.Value.AlternativeTitles = new List<AlternativeTitle> { new AlternativeTitle("Fack Ju Göthe 2: Same same") })
                                   .With(m => m.GameMetadata.Value.Translations = new List<GameTranslation> { new GameTranslation { Title = "Translated Title", CleanTitle = "translatedtitle" } })
                                   .With(m => m.GameMetadata.Value.OriginalLanguage = Language.English)
                                   .Build();

            _parsedGameInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { _game.Title },
                Languages = new List<Language> { Language.English },
                Year = _game.Year,
            };

            _wrongYearInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { _game.Title },
                Languages = new List<Language> { Language.English },
                Year = 1900,
            };

            _wrongTitleInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Other Title" },
                Languages = new List<Language> { Language.English },
                Year = 2015
            };

            _alternativeTitleInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { _game.GameMetadata.Value.AlternativeTitles.First().Title },
                Languages = new List<Language> { Language.English },
                Year = _game.Year,
            };

            _translationTitleInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { _game.GameMetadata.Value.Translations.First().Title },
                Languages = new List<Language> { Language.English },
                Year = _game.Year,
            };

            _romanTitleInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Fack Ju Göthe II" },
                Languages = new List<Language> { Language.English },
                Year = _game.Year,
            };

            _umlautInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Fack Ju Goethe 2" },
                Languages = new List<Language> { Language.English },
                Year = _game.Year
            };

            _umlautAltInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Fack Ju Goethe 2: Same same" },
                Languages = new List<Language> { Language.English },
                Year = _game.Year
            };

            _multiLanguageInfo = new ParsedGameInfo
            {
                GameTitles = { _game.Title },
                Languages = new List<Language> { Language.Original, Language.French }
            };

            _multiLanguageWithOriginalInfo = new ParsedGameInfo
            {
                GameTitles = { _game.Title },
                Languages = new List<Language> { Language.Original, Language.French, Language.English }
            };

            _gameSearchCriteria = new GameSearchCriteria
            {
                Game = _game
            };
        }

        private void GivenMatchByGameTitle()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitle(It.IsAny<string>()))
                  .Returns(_game);
        }

        [Test]
        public void should_lookup_Game_by_name()
        {
            GivenMatchByGameTitle();

            Subject.Map(_parsedGameInfo, 0, 0, null);

            Mocker.GetMock<IGameService>()
                .Verify(v => v.FindByTitle(It.IsAny<List<string>>(), It.IsAny<int>(), It.IsAny<List<string>>(), null), Times.Once());
        }

        [Test]
        public void should_use_search_criteria_game_title()
        {
            GivenMatchByGameTitle();

            Subject.Map(_parsedGameInfo, 0, 0, _gameSearchCriteria);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.FindByTitle(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_match_alternative_title()
        {
            Subject.Map(_alternativeTitleInfo, 0, 0, _gameSearchCriteria).Game.Should().Be(_gameSearchCriteria.Game);
        }

        [Test]
        public void should_match_translation_title()
        {
            Subject.Map(_translationTitleInfo, 0, 0, _gameSearchCriteria).Game.Should().Be(_gameSearchCriteria.Game);
        }

        [Test]
        public void should_match_roman_title()
        {
            Subject.Map(_romanTitleInfo, 0, 0, _gameSearchCriteria).Game.Should().Be(_gameSearchCriteria.Game);
        }

        [Test]
        public void should_match_umlauts()
        {
            Subject.Map(_umlautInfo, 0, 0, _gameSearchCriteria).Game.Should().Be(_gameSearchCriteria.Game);
            Subject.Map(_umlautAltInfo, 0, 0, _gameSearchCriteria).Game.Should().Be(_gameSearchCriteria.Game);
        }
    }
}
