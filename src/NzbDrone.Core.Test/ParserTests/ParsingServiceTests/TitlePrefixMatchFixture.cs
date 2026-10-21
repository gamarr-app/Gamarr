using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
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
    /// A search for "Super Mario Party" returned the releases of "Super Mario
    /// Party Jamboree" - a different game, its own IGDB entry - and the
    /// title-prefix rule handed them all to the searched game: the base release
    /// was grabbed and imported into the "Super Mario Party" folder, then 17 of
    /// the other game's updates were approved as upgrades for it (#206).
    ///
    /// The prefix rule is only allowed to claim a release whose title continues
    /// into an ADD-ON MARKER where the game's title ends ("... Update v1.1.0",
    /// "... Wave 1 DLC"), and never over a game in the library that matches
    /// more of that title.
    /// </summary>
    [TestFixture]
    public class TitlePrefixMatchFixture : TestBase<ParsingService>
    {
        private const string BaseTitle = "Super Mario Party";
        private const string OtherTitle = "Super Mario Party Jamboree";

        private Game _game;
        private Game _otherGame;
        private GameSearchCriteria _searchCriteria;

        [SetUp]
        public void Setup()
        {
            _game = GivenGame(1, BaseTitle);
            _otherGame = GivenGame(2, OtherTitle);

            _searchCriteria = new GameSearchCriteria { Game = _game };
        }

        private Game GivenGame(int id, string title)
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

        private static ParsedGameInfo GivenParsedInfo(string gameTitle, ReleaseContentType contentType)
        {
            return new ParsedGameInfo
            {
                GameTitles = new List<string> { gameTitle },
                ReleaseTitle = gameTitle,
                Languages = new List<Language> { Language.English },
                ContentType = contentType,
                Year = 0
            };
        }

        // The release from the issue: a plain base release of the OTHER game,
        // no Update or DLC marker anywhere in the name.
        [Test]
        public void should_not_match_another_games_base_release_to_the_searched_game()
        {
            var parsed = Parse($"{OtherTitle} NSW-VENOM");

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().BeNull();
        }

        // And its updates - the 17 that were approved as upgrades.
        [Test]
        public void should_not_match_another_games_update_to_the_searched_game()
        {
            var parsed = Parse($"{OtherTitle} Update v2.3.0 NSW-VENOM");

            parsed.ContentType.Should().Be(ReleaseContentType.UpdateOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().BeNull();
        }

        // The point of the prefix rule, which has to keep working: an update of
        // the searched game itself, whose title is the game's title plus a
        // marker.
        [Test]
        public void should_still_match_an_update_of_the_searched_game()
        {
            var parsed = Parse($"{BaseTitle} Update v1.1.0 NSW-VENOM");

            parsed.ContentType.Should().Be(ReleaseContentType.UpdateOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().Be(_game);
        }

        [Test]
        public void should_still_match_a_dlc_release_of_the_searched_game()
        {
            var parsed = Parse($"{BaseTitle} Wave 1 DLC NSW-VENOM");

            parsed.ContentType.Should().Be(ReleaseContentType.DlcOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().Be(_game);
        }

        // A DLC release names the DLC after the marker; the remainder still
        // starts with the marker, so it is still an add-on of the game.
        [Test]
        public void should_still_match_a_named_dlc_release_of_the_searched_game()
        {
            var parsed = GivenParsedInfo($"{BaseTitle} DLC Mario Mountain", ReleaseContentType.DlcOnly);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().Be(_game);
        }

        // Prefer the closer title match: the other game is in the library and
        // matches more of the release's title, so it owns the release even
        // though the searched game's title is a prefix of it too - the search
        // does not get to claim it first.
        [Test]
        public void should_prefer_the_game_that_matches_more_of_the_title_over_the_searched_game()
        {
            GivenLibrary(_game, _otherGame);

            var parsed = GivenParsedInfo($"{OtherTitle} DLC Pack", ReleaseContentType.SeasonPass);

            Subject.Map(parsed, 0, 0, _searchCriteria).Game.Should().Be(_otherGame);
        }

        // Same preference without search criteria (the RSS path): the longer
        // title wins, and an exact title match wins outright.
        [Test]
        public void should_prefer_an_exact_title_match_over_a_prefix_match_from_the_library()
        {
            GivenLibrary(_game, _otherGame);

            var parsed = GivenParsedInfo(OtherTitle, ReleaseContentType.UpdateOnly);

            Subject.Map(parsed, 0, 0, null).Game.Should().Be(_otherGame);
        }

        [Test]
        public void should_match_the_longer_title_for_an_addon_of_the_other_game_from_the_library()
        {
            GivenLibrary(_game, _otherGame);

            var parsed = GivenParsedInfo($"{OtherTitle} Update v2.3.0", ReleaseContentType.UpdateOnly);

            Subject.Map(parsed, 0, 0, null).Game.Should().Be(_otherGame);
        }

        // The library path must apply the marker rule too: with only the short
        // title in the library, the other game's update belongs to nobody.
        [Test]
        public void should_not_match_another_games_update_from_the_library_by_prefix()
        {
            GivenLibrary(_game);

            var parsed = GivenParsedInfo($"{OtherTitle} Update v2.3.0", ReleaseContentType.UpdateOnly);

            Subject.Map(parsed, 0, 0, null).Game.Should().BeNull();
        }
    }
}
