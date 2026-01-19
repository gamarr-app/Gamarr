using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Test.Common;
using Gamarr.Api.V3.Games;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class GameEditorFixture : IntegrationTest
    {
        private void GivenExistingGame()
        {
            WaitForCompletion(() => QualityProfiles.All().Count > 0);

            foreach (var title in new[] { "The Dark Knight", "Pulp Fiction" })
            {
                var newGame = Games.Lookup(title).First();

                newGame.QualityProfileId = 1;
                newGame.Path = string.Format(@"C:\Test\{0}", title).AsOsAgnostic();

                Games.Post(newGame);
            }
        }

        [Test]
        public void should_be_able_to_update_multiple_games()
        {
            GivenExistingGame();

            var games = Games.All();

            var gameEditor = new GameEditorResource
            {
                QualityProfileId = 2,
                GameIds = games.Select(o => o.Id).ToList()
            };

            var result = Games.Editor(gameEditor);

            result.Should().HaveCount(2);
            result.TrueForAll(s => s.QualityProfileId == 2).Should().BeTrue();
        }
    }
}
