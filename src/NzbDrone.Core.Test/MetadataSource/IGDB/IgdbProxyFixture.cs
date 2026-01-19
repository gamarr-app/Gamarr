using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.MetadataSource.IGDB;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSource.IGDB
{
    [TestFixture]
    [IntegrationTest]
    public class IgdbProxyFixture : CoreTest<IgdbProxy>
    {
        private Mock<IIgdbAuthService> _authService;
        private Mock<IConfigService> _configService;
        private Mock<IGameService> _gameService;
        private Mock<IGameMetadataService> _gameMetadataService;
        private Mock<IGameTranslationService> _gameTranslationService;

        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            _authService = new Mock<IIgdbAuthService>();
            _configService = new Mock<IConfigService>();
            _gameService = new Mock<IGameService>();
            _gameMetadataService = new Mock<IGameMetadataService>();
            _gameTranslationService = new Mock<IGameTranslationService>();

            // Note: These tests require valid IGDB credentials
            // Set up mock auth service to return a valid token if available
            _authService.Setup(s => s.GetAccessToken()).Returns(string.Empty);
            _authService.Setup(s => s.ClientId).Returns(string.Empty);
        }

        [Test]
        [Ignore("Requires valid IGDB credentials")]
        public void should_be_able_to_get_game_by_igdb_id()
        {
            // IGDB ID 1942 is The Witcher 3
            var result = Subject.GetGameInfo(1942);

            result.Should().NotBeNull();
            result.Item1.Should().NotBeNull();

            var game = result.Item1;
            ValidateGame(game);

            game.Title.Should().Contain("Witcher");
            game.IgdbId.Should().Be(1942);
        }

        [Test]
        [Ignore("Requires valid IGDB credentials")]
        public void should_be_able_to_get_popular_games()
        {
            var result = Subject.GetPopularGames();

            result.Should().NotBeNull();
            result.Should().NotBeEmpty();

            foreach (var game in result)
            {
                ValidateGame(game);
            }
        }

        [Test]
        [Ignore("Requires valid IGDB credentials")]
        public void should_be_able_to_get_trending_games()
        {
            var result = Subject.GetTrendingGames();

            result.Should().NotBeNull();

            // Trending games may be empty if no games match the criteria
        }

        [Test]
        public void should_return_empty_list_when_no_access_token()
        {
            _authService.Setup(s => s.GetAccessToken()).Returns(string.Empty);

            var result = Subject.GetPopularGames();

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreErrors();
        }

        [Test]
        public void should_return_null_for_imdb_id_lookup()
        {
            // IGDB doesn't support IMDb ID lookup directly
            var result = Subject.GetGameByImdbId("tt0000000");

            result.Should().BeNull();

            ExceptionVerification.IgnoreWarns();
        }

        private void ValidateGame(GameMetadata game)
        {
            game.Should().NotBeNull();
            game.Title.Should().NotBeNullOrWhiteSpace();
            game.CleanTitle.Should().NotBeNullOrWhiteSpace();
            game.IgdbId.Should().BeGreaterThan(0);
        }
    }
}
