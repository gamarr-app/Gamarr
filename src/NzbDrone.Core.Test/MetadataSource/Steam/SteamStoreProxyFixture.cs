using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.MetadataSource.Steam;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSource.Steam
{
    [TestFixture]
    [ExternalIntegrationTest]
    public class SteamStoreProxyFixture : CoreTest<SteamStoreProxy>
    {
        [SetUp]
        public void Setup()
        {
            UseRealHttp();
        }

        [TestCase(620, "Portal 2")]
        [TestCase(730, "Counter-Strike 2")]
        [TestCase(570, "Dota 2")]
        [TestCase(1245620, "ELDEN RING")]
        public void should_be_able_to_get_game_detail_by_steam_app_id(int steamAppId, string expectedTitle)
        {
            var result = Subject.GetGameInfo(steamAppId);

            result.Should().NotBeNull();

            var game = result;
            ValidateGame(game);

            game.Title.Should().Contain(expectedTitle.Split(' ')[0]);
            game.SteamAppId.Should().Be(steamAppId);
        }

        [Test]
        public void should_return_null_for_invalid_steam_app_id()
        {
            var result = Subject.GetGameInfo(99999999);

            result.Should().BeNull();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_return_null_for_dlc()
        {
            // Steam App ID 323170 is typically a DLC, not a game
            // The proxy should filter out non-game content
            var result = Subject.GetGameInfo(323170);

            // DLC returns success=false from Steam API
            result.Should().BeNull();

            ExceptionVerification.IgnoreWarns();
        }

        private void ValidateGame(GameMetadata game)
        {
            game.Should().NotBeNull();
            game.Title.Should().NotBeNullOrWhiteSpace();
            game.CleanTitle.Should().NotBeNullOrWhiteSpace();
            game.SortTitle.Should().NotBeNullOrWhiteSpace();
            game.Overview.Should().NotBeNullOrWhiteSpace();
            game.SteamAppId.Should().BeGreaterThan(0);
            game.Images.Should().NotBeEmpty();
            game.Status.Should().NotBe(GameStatusType.TBA);
        }
    }
}
