using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class GameLookupFixture : IntegrationTest
    {
        [TestCase("half-life", "Half-Life 2")]
        [TestCase("portal", "Portal")]
        public void lookup_new_game_by_title(string term, string title)
        {
            var game = Games.Lookup(term);

            game.Should().NotBeEmpty();
            game.Should().Contain(c => c.Title == title);
        }

        [Test]
        public void lookup_new_game_by_igdbid()
        {
            var game = Games.Lookup("igdb:21");

            game.Should().NotBeEmpty();
            game.Should().Contain(c => c.Title == "Half-Life 2");
        }

        [Test]
        public void lookup_new_game_by_steam_prefix()
        {
            // Steam App ID 400 is Portal
            var game = Games.Lookup("steam:400");

            game.Should().NotBeEmpty();
            game.Should().Contain(c => c.Title == "Portal");
        }

        [Test]
        [Ignore("Unreliable")]
        public void lookup_random_game_using_asterix()
        {
            var game = Games.Lookup("*");

            game.Should().NotBeEmpty();
        }
    }
}
