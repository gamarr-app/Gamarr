using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class GameDuplicateValidatorFixture : CoreTest<GameDuplicateValidator>
    {
        private static Game GivenGame(int id, int igdbId, int steamAppId, PlatformFamily platform, string title = "Zelda Ocarina of Time", int year = 2026)
        {
            return new Game
            {
                Id = id,
                Platform = platform,
                GameMetadata = new GameMetadata
                {
                    IgdbId = igdbId,
                    SteamAppId = steamAppId,
                    Title = title,
                    CleanTitle = title.CleanGameTitle(),
                    Year = year
                }
            };
        }

        private void GivenLibrary(params Game[] games)
        {
            Mocker.GetMock<IGameService>()
                .Setup(s => s.FindByIgdbIdOrSteamAppId(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int igdbId, int steamAppId) => new List<Game>(games).FindAll(g => (igdbId > 0 && g.IgdbId == igdbId) || (steamAppId > 0 && g.SteamAppId == steamAppId)));

            Mocker.GetMock<IGameService>()
                .Setup(s => s.FindByTitleCandidates(It.IsAny<List<string>>(), out It.Ref<List<string>>.IsAny))
                .Returns(new List<Game>(games));
        }

        [Test]
        public void should_reject_same_platform_duplicate()
        {
            GivenLibrary(GivenGame(1, 405460, 0, PlatformFamily.NintendoSwitch));

            Subject.FindDuplicate(GivenGame(0, 405460, 0, PlatformFamily.NintendoSwitch)).Should().NotBeNull();
        }

        [Test]
        public void should_reject_unknown_platform_add_when_title_exists()
        {
            GivenLibrary(GivenGame(1, 405460, 0, PlatformFamily.NintendoSwitch));

            Subject.FindDuplicate(GivenGame(0, 405460, 0, PlatformFamily.Unknown)).Should().NotBeNull();
        }

        [Test]
        public void should_allow_same_title_on_another_platform()
        {
            GivenLibrary(GivenGame(1, 405460, 0, PlatformFamily.NintendoSwitch));

            Subject.FindDuplicate(GivenGame(0, 405460, 0, PlatformFamily.NintendoN64)).Should().BeNull();
        }

        [Test]
        public void should_allow_platformed_add_next_to_legacy_unknown_entry()
        {
            GivenLibrary(GivenGame(1, 405460, 0, PlatformFamily.Unknown));

            Subject.FindDuplicate(GivenGame(0, 405460, 0, PlatformFamily.PC)).Should().BeNull();
        }

        [Test]
        public void should_reject_steam_add_of_a_game_added_from_igdb()
        {
            GivenLibrary(GivenGame(1, 405460, 0, PlatformFamily.PC));

            Subject.FindDuplicate(GivenGame(0, 0, 12345, PlatformFamily.PC)).Should().NotBeNull();
        }

        [Test]
        public void should_not_match_other_igdb_entry_by_title()
        {
            GivenLibrary(GivenGame(1, 1029, 0, PlatformFamily.PC));

            Subject.FindDuplicate(GivenGame(0, 405460, 0, PlatformFamily.PC)).Should().BeNull();
        }

        [Test]
        public void should_not_match_cross_source_title_with_a_different_year()
        {
            GivenLibrary(GivenGame(1, 405460, 0, PlatformFamily.PC, year: 1998));

            Subject.FindDuplicate(GivenGame(0, 0, 12345, PlatformFamily.PC)).Should().BeNull();
        }

        [Test]
        public void should_not_match_itself()
        {
            var game = GivenGame(1, 405460, 0, PlatformFamily.PC);
            GivenLibrary(game);

            Subject.FindDuplicate(game).Should().BeNull();
        }
    }
}
