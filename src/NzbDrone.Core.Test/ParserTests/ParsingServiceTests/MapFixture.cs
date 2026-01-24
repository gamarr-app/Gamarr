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

        [Test]
        public void should_match_by_word_fallback_when_dlc_name_in_parentheses()
        {
            var game = Builder<Game>.CreateNew()
                .With(m => m.Title = "Factorio: Space Age")
                .With(m => m.GameMetadata.Value.Title = "Factorio: Space Age")
                .With(m => m.GameMetadata.Value.OriginalTitle = "Factorio: Space Age")
                .With(m => m.GameMetadata.Value.CleanTitle = "factoriospaceage")
                .With(m => m.GameMetadata.Value.CleanOriginalTitle = "factoriospaceage")
                .With(m => m.GameMetadata.Value.AlternativeTitles = new List<AlternativeTitle>())
                .With(m => m.GameMetadata.Value.Translations = new List<GameTranslation>())
                .With(m => m.Year = 2024)
                .Build();

            var searchCriteria = new GameSearchCriteria { Game = game };

            // Parser extracts just "Factorio" but release title has "Space Age" in parentheses
            var parsedInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Factorio" },
                ReleaseTitle = "Factorio (v2.0.7 + Space Age DLC + Bonus Soundtrack, MULTi41) [FitGirl Repack]",
                Languages = new List<Language> { Language.English },
                Year = 0
            };

            Subject.Map(parsedInfo, 0, 0, searchCriteria).Game.Should().Be(game);
        }

        [Test]
        public void should_not_match_by_word_fallback_with_single_word_title()
        {
            var game = Builder<Game>.CreateNew()
                .With(m => m.Title = "Factorio")
                .With(m => m.GameMetadata.Value.Title = "Factorio")
                .With(m => m.GameMetadata.Value.OriginalTitle = "Factorio")
                .With(m => m.GameMetadata.Value.CleanTitle = "factorio")
                .With(m => m.GameMetadata.Value.CleanOriginalTitle = "factorio")
                .With(m => m.GameMetadata.Value.AlternativeTitles = new List<AlternativeTitle>())
                .With(m => m.GameMetadata.Value.Translations = new List<GameTranslation>())
                .With(m => m.Year = 2020)
                .Build();

            var searchCriteria = new GameSearchCriteria { Game = game };

            // Single-word title should NOT use word fallback (requires >= 2 words)
            var parsedInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Other Game" },
                ReleaseTitle = "Other Game with Factorio mod",
                Languages = new List<Language> { Language.English },
                Year = 0
            };

            Subject.Map(parsedInfo, 0, 0, searchCriteria).Game.Should().BeNull();
        }

        [Test]
        public void should_match_by_steam_app_id()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindBySteamAppId(440))
                  .Returns(_game);

            var result = Subject.Map(_parsedGameInfo, 440, 0, null);

            result.Game.Should().Be(_game);
            result.GameMatchType.Should().Be(GameMatchType.Id);
        }

        [Test]
        public void should_match_by_igdb_id_when_steam_not_found()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindBySteamAppId(It.IsAny<int>()))
                  .Returns((Game)null);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByIgdbId(1234))
                  .Returns(_game);

            var result = Subject.Map(_parsedGameInfo, 0, 1234, null);

            result.Game.Should().Be(_game);
            result.GameMatchType.Should().Be(GameMatchType.Id);
        }

        [Test]
        public void should_prefer_steam_id_over_igdb_id()
        {
            var otherGame = Builder<Game>.CreateNew()
                .With(m => m.Title = "Other Game")
                .With(m => m.Year = 2020)
                .Build();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindBySteamAppId(440))
                  .Returns(_game);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByIgdbId(1234))
                  .Returns(otherGame);

            var result = Subject.Map(_parsedGameInfo, 440, 1234, null);

            result.Game.Should().Be(_game);
        }

        [Test]
        public void should_not_match_by_steam_id_when_year_mismatch()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindBySteamAppId(440))
                  .Returns(_game);

            var result = Subject.Map(_wrongYearInfo, 440, 0, null);

            result.Game.Should().BeNull();
        }

        [Test]
        public void should_match_by_steam_id_when_no_year_in_parsed_info()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindBySteamAppId(440))
                  .Returns(_game);

            var noYearInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Some Title" },
                Languages = new List<Language> { Language.English },
                Year = 0
            };

            var result = Subject.Map(noYearInfo, 440, 0, null);

            result.Game.Should().Be(_game);
        }

        [Test]
        public void should_map_by_game_id()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(42))
                  .Returns(_game);

            var result = Subject.Map(_parsedGameInfo, 42);

            result.Game.Should().Be(_game);
            result.ParsedGameInfo.Should().Be(_parsedGameInfo);
        }

        [Test]
        public void should_set_game_requested_when_search_criteria_matches()
        {
            var result = Subject.Map(_parsedGameInfo, 0, 0, _gameSearchCriteria);

            result.GameRequested.Should().BeTrue();
        }

        [Test]
        public void should_not_set_game_requested_when_different_game_found()
        {
            var otherGame = Builder<Game>.CreateNew()
                .With(m => m.Id = 999)
                .With(m => m.Title = "Other Game")
                .With(m => m.GameMetadata.Value.CleanTitle = "othergame")
                .With(m => m.Year = 2020)
                .With(m => m.GameMetadata.Value.AlternativeTitles = new List<AlternativeTitle>())
                .With(m => m.GameMetadata.Value.Translations = new List<GameTranslation>())
                .Build();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindBySteamAppId(440))
                  .Returns(otherGame);

            // Use a parsedInfo with a non-matching title so search criteria title match
            // doesn't override the Steam ID lookup. Year=0 allows Steam ID match to succeed.
            var noMatchInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Completely Different" },
                Languages = new List<Language> { Language.English },
                Year = 0
            };

            var result = Subject.Map(noMatchInfo, 440, 0, _gameSearchCriteria);

            result.GameRequested.Should().BeFalse();
        }

        [Test]
        public void should_set_languages_from_parsed_info()
        {
            var result = Subject.Map(_multiLanguageInfo, 0, 0, _gameSearchCriteria);

            result.Languages.Should().Contain(Language.Original);
            result.Languages.Should().Contain(Language.French);
        }

        [Test]
        public void should_match_by_steam_id_in_search_criteria()
        {
            _game.GameMetadata.Value.SteamAppId = 440;

            var noMatchInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Completely Different" },
                Languages = new List<Language> { Language.English },
                Year = 0
            };

            var result = Subject.Map(noMatchInfo, 440, 0, _gameSearchCriteria);

            result.Game.Should().Be(_game);
            result.GameMatchType.Should().Be(GameMatchType.Id);
        }

        [Test]
        public void should_match_by_igdb_id_in_search_criteria()
        {
            _game.GameMetadata.Value.IgdbId = 5678;

            var noMatchInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Completely Different" },
                Languages = new List<Language> { Language.English },
                Year = 0
            };

            var result = Subject.Map(noMatchInfo, 0, 5678, _gameSearchCriteria);

            result.Game.Should().Be(_game);
            result.GameMatchType.Should().Be(GameMatchType.Id);
        }

        [Test]
        public void should_return_null_game_when_no_match_found()
        {
            var result = Subject.Map(_wrongTitleInfo, 0, 0, null);

            result.Game.Should().BeNull();
        }
    }
}
