using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    /// <summary>
    /// A search for "Aaaaaaaa 2" approved 18 update releases of "Aaaaaaaa 3" —
    /// a different game, its own IGDB entry, not even in the library — and one
    /// of them was grabbed automatically (#211).
    ///
    /// The all-words fallback split the WHOLE release name into words, so the
    /// "2" the searched game's title needs came out of the update's version
    /// number: "Aaaaaaaa 3 Update v11.2.0 NSW-GRP" contains "aaaaaaaa" and "2".
    /// That is also why the same game's releases were mapped inconsistently -
    /// its base and DLC releases had no "2" anywhere in their names - and why
    /// nothing was logged: that fallback returned a match without a reason.
    /// </summary>
    [TestFixture]
    public class SequelTitleMatchFixture : TestBase<ParsingService>
    {
        private const string SearchedTitle = "Aaaaaaaa 2";
        private const string SequelTitle = "Aaaaaaaa 3";

        private Game _game;
        private GameSearchCriteria _searchCriteria;
        private MemoryTarget _logTarget;

        [SetUp]
        public void Setup()
        {
            _game = GivenGame(1, SearchedTitle);
            _searchCriteria = new GameSearchCriteria { Game = _game };

            GivenLibrary(_game);

            _logTarget = new MemoryTarget { Name = "SequelTitleMatchFixture", Layout = "${message}" };

            LogManager.Configuration.AddTarget(_logTarget.Name, _logTarget);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, _logTarget));
            LogManager.ReconfigExistingLoggers();
        }

        [TearDown]
        public void RemoveLogTarget()
        {
            LogManager.Configuration.LoggingRules.Remove(
                LogManager.Configuration.LoggingRules.FirstOrDefault(r => r.Targets.Contains(_logTarget)));
            LogManager.Configuration.RemoveTarget(_logTarget.Name);
            LogManager.ReconfigExistingLoggers();
        }

        private static Game GivenGame(int id, string title)
        {
            return Builder<Game>.CreateNew()
                .With(g => g.Id = id)
                .With(g => g.Title = title)
                .With(g => g.GameMetadata.Value.Title = title)
                .With(g => g.GameMetadata.Value.OriginalTitle = title)
                .With(g => g.GameMetadata.Value.CleanTitle = title.CleanGameTitle())
                .With(g => g.GameMetadata.Value.CleanOriginalTitle = title.CleanGameTitle())
                .With(g => g.GameMetadata.Value.AlternativeTitles = new List<AlternativeTitle>())
                .With(g => g.GameMetadata.Value.Translations = new List<GameTranslation>())
                .With(g => g.Year = 0)
                .Build();
        }

        private void GivenLibrary(params Game[] games)
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetAllGames())
                  .Returns(new List<Game>(games));
        }

        private static ParsedGameInfo Parse(string releaseTitle)
        {
            var parsed = NzbDrone.Core.Parser.Parser.ParseGameTitle(releaseTitle);

            parsed.Should().NotBeNull("the release name has to parse for the match to be the thing under test");

            return parsed;
        }

        private static ParsedGameInfo GivenParsedInfo(string gameTitle, string releaseTitle, ReleaseContentType contentType)
        {
            return new ParsedGameInfo
            {
                GameTitles = new List<string> { gameTitle },
                ReleaseTitle = releaseTitle,
                Languages = new List<Language> { Language.English },
                ContentType = contentType,
                Year = 0
            };
        }

        // The release from the issue, with the version number that supplied the
        // searched game's sequel number.
        [Test]
        public void should_not_match_a_sequels_update_to_the_searched_game()
        {
            var parsed = Parse($"{SequelTitle} Update v11.2.0 NSW-VENOM");

            parsed.PrimaryGameTitle.Should().Be(SequelTitle);
            parsed.ContentType.Should().Be(ReleaseContentType.UpdateOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().BeNull();
        }

        // Same game, DLC instead of an update: these were already rejected,
        // and the point of the fix is that every content type now agrees.
        [Test]
        public void should_not_match_a_sequels_dlc_to_the_searched_game()
        {
            var parsed = GivenParsedInfo(SequelTitle, $"{SequelTitle} DLC Pack v2.1.0 NSW-VENOM", ReleaseContentType.DlcOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().BeNull();
        }

        // And its base release, whose name carries a version number too.
        [Test]
        public void should_not_match_a_sequels_base_release_to_the_searched_game()
        {
            var parsed = GivenParsedInfo(SequelTitle, $"{SequelTitle} v1.2.0 NSW-VENOM", ReleaseContentType.BaseGame);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().BeNull();
        }

        // Not only the version number: any bracketed segment the fallback is
        // allowed to read can hold the sequel's number, so the trailing-number
        // rule stands on its own.
        [Test]
        public void should_not_match_a_sequel_whose_bracketed_text_holds_the_searched_number()
        {
            var parsed = GivenParsedInfo(SequelTitle, $"{SequelTitle} (Update 2) NSW-VENOM", ReleaseContentType.UpdateOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().BeNull();
        }

        // The RSS path has no search criteria: the sequel's update belongs to
        // nobody when only "Aaaaaaaa 2" is in the library.
        [Test]
        public void should_not_match_a_sequels_update_from_the_library_on_the_rss_path()
        {
            var parsed = Parse($"{SequelTitle} Update v11.2.0 NSW-VENOM");

            Subject.Map(parsed, 0, 0, null).Game.Should().BeNull();
        }

        [Test]
        public void should_not_match_a_sequels_dlc_from_the_library_on_the_rss_path()
        {
            var parsed = GivenParsedInfo(SequelTitle, $"{SequelTitle} DLC Pack v2.1.0 NSW-VENOM", ReleaseContentType.DlcOnly);

            Subject.Map(parsed, 0, 0, null).Game.Should().BeNull();
        }

        // The legitimate case has to keep working: an update of the searched
        // game itself.
        [Test]
        public void should_still_match_an_update_of_the_searched_game()
        {
            var parsed = Parse($"{SearchedTitle} Update v11.2.0 NSW-VENOM");

            parsed.ContentType.Should().Be(ReleaseContentType.UpdateOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().Be(_game);
        }

        [Test]
        public void should_still_match_a_dlc_release_of_the_searched_game()
        {
            var parsed = Parse($"{SearchedTitle} Wave 1 DLC NSW-VENOM");

            parsed.ContentType.Should().Be(ReleaseContentType.DlcOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().Be(_game);
        }

        // The trailing-number rule only fires when the two titles are the same
        // name with different numbers. A game whose own title ends in a number
        // still gets the DLC releases that name a wave or a pack after it, via
        // the fallback, with the number coming out of the brackets.
        [Test]
        public void should_still_match_a_dlc_whose_bracketed_name_holds_the_games_own_number()
        {
            var game = GivenGame(2, "Mario Kart 8");

            var parsed = GivenParsedInfo("Mario Kart", "Mario Kart (8 Deluxe Booster Course Pass Wave 1 DLC)", ReleaseContentType.DlcOnly);

            Subject.Map(parsed, 0, 0, new GameSearchCriteria { Game = game }).Game.Should().Be(game);
        }

        // The silent approval was half the bug: a mapped release has to say
        // which game it was mapped to, why, and as what content type.
        [Test]
        public void should_log_the_match_reason_for_an_update_of_the_searched_game()
        {
            var parsed = Parse($"{SearchedTitle} Update v11.2.0 NSW-VENOM");

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().Be(_game);

            _logTarget.Logs.Should().Contain(l => l.Contains("Matched release") &&
                                                  l.Contains(SearchedTitle) &&
                                                  l.Contains(nameof(ReleaseContentType.UpdateOnly)));
        }
    }
}
