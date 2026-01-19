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
    public class IgdbProxySearchFixture : CoreTest<IgdbProxy>
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
            _authService.Setup(s => s.GetAccessToken()).Returns(string.Empty);
            _authService.Setup(s => s.ClientId).Returns(string.Empty);
        }

        [Test]
        [Ignore("Requires valid IGDB credentials")]
        [TestCase("The Witcher 3")]
        [TestCase("Portal 2")]
        [TestCase("Elden Ring")]
        [TestCase("Cyberpunk 2077")]
        public void should_be_able_to_search_for_game_by_title(string title)
        {
            var result = Subject.SearchForNewGame(title);

            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Test]
        [Ignore("Requires valid IGDB credentials")]
        public void should_be_able_to_search_by_igdb_id_prefix()
        {
            // Search with igdb: prefix for direct ID lookup
            var result = Subject.SearchForNewGame("igdb:1942");

            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result[0].GameMetadata.Value.IgdbId.Should().Be(1942);
        }

        [Test]
        [Ignore("Requires valid IGDB credentials")]
        public void should_be_able_to_search_by_igdbid_prefix()
        {
            // Search with igdbid: prefix for direct ID lookup
            var result = Subject.SearchForNewGame("igdbid:1942");

            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result[0].GameMetadata.Value.IgdbId.Should().Be(1942);
        }

        [Test]
        public void should_return_empty_list_when_no_access_token()
        {
            _authService.Setup(s => s.GetAccessToken()).Returns(string.Empty);

            var result = Subject.SearchForNewGame("Test Game");

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreErrors();
        }

        [Test]
        public void should_return_empty_list_for_invalid_igdb_id()
        {
            var result = Subject.SearchForNewGame("igdb:999999999");

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreErrors();
        }
    }
}
