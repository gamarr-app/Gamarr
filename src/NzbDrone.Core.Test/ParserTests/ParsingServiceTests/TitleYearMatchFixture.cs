using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    /// <summary>
    /// A game whose own name ends in a year-like number - "Mario &amp; Sonic at
    /// the Olympic Games Tokyo 2020", an IGDB entry that came out in 2019 - was
    /// unmatchable (#209). The parser reads the number as the release year, so
    /// the title it hands over has LOST it and the year it hands over is not the
    /// game's year: the title check misses and the year gate rejects.
    ///
    /// So the number is put back on the title and that is looked up first. It
    /// has to produce the game's WHOLE clean title, which is why this is not a
    /// loosening of the prefix rule #206 tightened - a release of a DIFFERENT
    /// game whose title merely starts with the searched game's still belongs to
    /// nobody.
    /// </summary>
    [TestFixture]
    public class TitleYearMatchFixture : TestBase<ParsingService>
    {
        private const string YearTitle = "Mario & Sonic at the Olympic Games Tokyo 2020";
        private const string BaseTitle = "Super Mario Party";
        private const string OtherTitle = "Super Mario Party Jamboree";

        private Game _yearGame;
        private Game _baseGame;

        [SetUp]
        public void Setup()
        {
            // Released in 2019, named 2020 - the whole point of the case.
            _yearGame = GivenGame(1, YearTitle, 2019);
            _baseGame = GivenGame(2, BaseTitle, 0);
        }

        private Game GivenGame(int id, string title, int year)
        {
            return Builder<Game>.CreateNew()
                .With(g => g.Id = id)
                .With(g => g.Title = title)
                .With(g => g.Year = year)
                .With(g => g.GameMetadata.Value.Title = title)
                .With(g => g.GameMetadata.Value.OriginalTitle = title)
                .With(g => g.GameMetadata.Value.CleanTitle = title.CleanGameTitle())
                .With(g => g.GameMetadata.Value.CleanOriginalTitle = title.CleanGameTitle())
                .With(g => g.GameMetadata.Value.AlternativeTitles = new List<AlternativeTitle>())
                .With(g => g.GameMetadata.Value.Translations = new List<GameTranslation>())
                .With(g => g.GameMetadata.Value.Year = year)
                .With(g => g.GameMetadata.Value.SecondaryYear = null)
                .Build();
        }

        private static ParsedGameInfo Parse(string releaseTitle)
        {
            var parsed = NzbDrone.Core.Parser.Parser.ParseGameTitle(releaseTitle);

            parsed.Should().NotBeNull("the release name has to parse for the match to be the thing under test");

            return parsed;
        }

        // The library/RSS path: the lookup that finds the game is the one given
        // the titles WITH the number still on them, not the title+year one.
        private void GivenFoundByTitlesIncludingYear(Game game, int year)
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitleCandidates(It.IsAny<List<string>>(), out It.Ref<List<string>>.IsAny))
                  .Returns(new List<Game> { game });

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitle(It.IsAny<List<string>>(), It.IsAny<int?>(), It.IsAny<List<string>>(), It.IsAny<List<Game>>()))
                  .Returns((Game)null);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitle(
                      It.Is<List<string>>(titles => titles.Any(t => t.EndsWith(year.ToString()))),
                      null,
                      It.IsAny<List<string>>(),
                      It.IsAny<List<Game>>()))
                  .Returns(game);
        }

        // The release from the issue, on the interactive-search path the
        // reporter used (GET /api/v3/release?gameId=...).
        [Test]
        public void should_match_a_release_whose_year_is_part_of_the_searched_games_title()
        {
            var parsed = Parse("Mario and Sonic at the Olympic Games Tokyo 2020 NSW-VENOM");

            parsed.Year.Should().Be(2020, "the parser still reads the number as the year - that is the input to the fix");
            parsed.GameTitle.Should().NotContain("2020");

            var searchCriteria = new GameSearchCriteria { Game = _yearGame };

            Subject.Map(parsed, 0, 0, searchCriteria).Game.Should().Be(_yearGame);
        }

        // And on the RSS path, which has no search criteria at all.
        [Test]
        public void should_match_a_release_whose_year_is_part_of_the_games_title_without_search_criteria()
        {
            GivenFoundByTitlesIncludingYear(_yearGame, 2020);

            var parsed = Parse("Mario and Sonic at the Olympic Games Tokyo 2020 NSW-VENOM");

            Subject.Map(parsed, 0, 0, null).Game.Should().Be(_yearGame);
        }

        // "&" in the game's name against "and" in the release name: the clean
        // title drops both, so the two forms have to come out identical.
        [Test]
        public void should_treat_an_ampersand_and_the_word_and_as_equivalent()
        {
            "Mario & Sonic at the Olympic Games Tokyo 2020".CleanGameTitle()
                .Should().Be("Mario and Sonic at the Olympic Games Tokyo 2020".CleanGameTitle());
        }

        // The ordinary case must be untouched: when the number really is the
        // release year, the title+year lookup is still what matches.
        [Test]
        public void should_still_match_a_release_whose_year_is_the_release_year()
        {
            var game = GivenGame(3, "Hollow Knight", 2017);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitleCandidates(It.IsAny<List<string>>(), out It.Ref<List<string>>.IsAny))
                  .Returns(new List<Game> { game });

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitle(It.IsAny<List<string>>(), It.IsAny<int?>(), It.IsAny<List<string>>(), It.IsAny<List<Game>>()))
                  .Returns((Game)null);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitle(It.IsAny<List<string>>(), 2017, It.IsAny<List<string>>(), It.IsAny<List<Game>>()))
                  .Returns(game);

            var parsed = Parse("Hollow Knight (2017)");

            parsed.Year.Should().Be(2017);
            parsed.GameTitle.Should().Be("Hollow Knight");

            Subject.Map(parsed, 0, 0, null).Game.Should().Be(game);
        }

        // #206, restated against the region-token fix: now that "EUR NSW" is
        // removed before the title is read, the release's title is EXACTLY the
        // other game's title - and the searched game must still not get it.
        [Test]
        public void should_not_match_another_games_console_base_release_to_the_searched_game()
        {
            var parsed = Parse($"{OtherTitle} EUR NSW-VENOM");

            parsed.GameTitle.Should().Be(OtherTitle);

            var searchCriteria = new GameSearchCriteria { Game = _baseGame };

            Subject.Map(parsed, 0, 0, searchCriteria).Game.Should().BeNull();
        }

        // The other half of #206 with the tags removed: the searched game's own
        // console base release still has to match it.
        [Test]
        public void should_match_the_searched_games_own_console_base_release()
        {
            var parsed = Parse($"{BaseTitle} EUR NSW-VENOM");

            parsed.GameTitle.Should().Be(BaseTitle);

            var searchCriteria = new GameSearchCriteria { Game = _baseGame };

            Subject.Map(parsed, 0, 0, searchCriteria).Game.Should().Be(_baseGame);
        }
    }
}
