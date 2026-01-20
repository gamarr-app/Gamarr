using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.MetadataSource.RAWG;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSource.RAWG
{
    [TestFixture]
    [IntegrationTest]
    public class RawgProxyFixture : CoreTest<RawgProxy>
    {
        private string _apiKey;

        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            // Get API key from environment for integration tests
            _apiKey = Environment.GetEnvironmentVariable("RAWG_API_KEY");

            if (string.IsNullOrEmpty(_apiKey))
            {
                Assert.Ignore("RAWG_API_KEY environment variable not set. Skipping integration tests.");
            }

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.RawgApiKey)
                .Returns(_apiKey);
        }

        [TestCase(3498, "Grand Theft Auto V")]
        [TestCase(3328, "The Witcher 3: Wild Hunt")]
        [TestCase(4200, "Portal 2")]
        public void should_be_able_to_get_game_detail(int rawgId, string expectedTitle)
        {
            var result = Subject.GetGameInfo(rawgId);

            result.Should().NotBeNull();

            var game = result;
            ValidateGame(game);

            game.Title.Should().Be(expectedTitle);
        }

        [Test]
        public void should_throw_for_invalid_id()
        {
            Action act = () => Subject.GetGameInfo(99999999);

            act.Should().Throw<Exception>();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_throw_when_api_key_not_configured()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.RawgApiKey)
                .Returns(string.Empty);

            Action act = () => Subject.GetGameInfo(3498);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*RAWG API key not configured*");

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_get_trending_games()
        {
            var result = Subject.GetTrendingGames();

            result.Should().NotBeEmpty();
            result.Should().HaveCountGreaterThan(0);

            foreach (var game in result)
            {
                game.Title.Should().NotBeNullOrWhiteSpace();
            }

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_get_popular_games()
        {
            var result = Subject.GetPopularGames();

            result.Should().NotBeEmpty();
            result.Should().HaveCountGreaterThan(0);

            foreach (var game in result)
            {
                game.Title.Should().NotBeNullOrWhiteSpace();
            }

            ExceptionVerification.IgnoreWarns();
        }

        private void ValidateGame(GameMetadata game)
        {
            game.Should().NotBeNull();
            game.Title.Should().NotBeNullOrWhiteSpace();
            game.CleanTitle.Should().NotBeNullOrWhiteSpace();
            game.SortTitle.Should().NotBeNullOrWhiteSpace();
            game.Overview.Should().NotBeNullOrWhiteSpace();
            game.IgdbId.Should().BeGreaterThan(0); // RAWG ID stored in IgdbId field
            game.Images.Should().NotBeEmpty();
            game.Genres.Should().NotBeEmpty();
            game.Platforms.Should().NotBeEmpty();
        }
    }
}
