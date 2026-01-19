using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class GameLookupFixture : IntegrationTest
    {
        [TestCase("psycho", "Psycho")]
        [TestCase("pulp fiction", "Pulp Fiction")]
        public void lookup_new_game_by_title(string term, string title)
        {
            var game = Games.Lookup(term);

            game.Should().NotBeEmpty();
            game.Should().Contain(c => c.Title == title);
        }

        [Test]
        public void lookup_new_game_by_imdbid()
        {
            var game = Games.Lookup("imdb:tt0110912");

            game.Should().NotBeEmpty();
            game.Should().Contain(c => c.Title == "Pulp Fiction");
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
