using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.SkyHook;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSource.SkyHook
{
    [TestFixture]
    [IntegrationTest]
    public class SkyHookProxyFixture : CoreTest<SkyHookProxy>
    {
        [SetUp]
        public void Setup()
        {
            UseRealHttp();
        }

        [TestCase(11, "Star Wars")]
        [TestCase(2, "Ariel")]
        [TestCase(70981, "Prometheus")]
        [TestCase(238, "The Godfather")]
        public void should_be_able_to_get_game_detail(int igdbId, string title)
        {
            var details = Subject.GetGameInfo(igdbId).Item1;

            ValidateGame(details);

            details.Title.Should().Be(title);
        }

        private void ValidateGame(GameMetadata game)
        {
            game.Should().NotBeNull();
            game.Title.Should().NotBeNullOrWhiteSpace();
            game.CleanTitle.Should().Be(Parser.Parser.CleanGameTitle(game.Title));
            game.SortTitle.Should().Be(GameTitleNormalizer.Normalize(game.Title, game.IgdbId));
            game.Overview.Should().NotBeNullOrWhiteSpace();
            game.InDevelopment.Should().HaveValue();
            game.Images.Should().NotBeEmpty();
            game.ImdbId.Should().NotBeNullOrWhiteSpace();
            game.Studio.Should().NotBeNullOrWhiteSpace();
            game.Runtime.Should().BeGreaterThan(0);
            game.IgdbId.Should().BeGreaterThan(0);
        }
    }
}
