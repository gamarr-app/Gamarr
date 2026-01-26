using System;
using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.MetadataSource.IGDB;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSource.IGDB
{
    [TestFixture]
    [ExternalIntegrationTest]
    public class IgdbProxyFixture : CoreTest<IgdbProxy>
    {
        private string _clientId;
        private string _accessToken;

        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            _clientId = Environment.GetEnvironmentVariable("IGDB_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("IGDB_CLIENT_SECRET");

            if (!string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                _accessToken = FetchAccessToken(_clientId, clientSecret);
            }

            Mocker.GetMock<IIgdbAuthService>()
                .Setup(s => s.GetAccessToken())
                .Returns(_accessToken ?? string.Empty);

            Mocker.GetMock<IIgdbAuthService>()
                .Setup(s => s.ClientId)
                .Returns(_clientId ?? string.Empty);
        }

        private void RequireCredentials()
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                Assert.Ignore("IGDB_CLIENT_ID and IGDB_CLIENT_SECRET environment variables not set or token fetch failed.");
            }
        }

        [Test]
        public void should_be_able_to_get_game_by_igdb_id()
        {
            RequireCredentials();

            // IGDB ID 1942 is The Witcher 3
            var result = Subject.GetGameInfoByIgdbId(1942);

            result.Should().NotBeNull();
            ValidateGame(result);
            result.Title.Should().Contain("Witcher");
            result.IgdbId.Should().Be(1942);
        }

        [Test]
        public void should_be_able_to_get_popular_games()
        {
            RequireCredentials();

            var result = Subject.GetPopularGames();

            result.Should().NotBeNull();
            result.Should().NotBeEmpty();

            foreach (var game in result)
            {
                ValidateGame(game);
            }
        }

        [Test]
        public void should_be_able_to_get_trending_games()
        {
            RequireCredentials();

            var result = Subject.GetTrendingGames();

            result.Should().NotBeNull();

            // Trending games may be empty if no games match the criteria
        }

        [Test]
        public void should_return_empty_list_when_no_access_token()
        {
            Mocker.GetMock<IIgdbAuthService>()
                .Setup(s => s.GetAccessToken())
                .Returns(string.Empty);

            var result = Subject.GetPopularGames();

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreErrors();
        }

        private void ValidateGame(GameMetadata game)
        {
            game.Should().NotBeNull();
            game.Title.Should().NotBeNullOrWhiteSpace();
            game.CleanTitle.Should().NotBeNullOrWhiteSpace();
            game.IgdbId.Should().BeGreaterThan(0);
        }

        private static string FetchAccessToken(string clientId, string clientSecret)
        {
            using (var httpClient = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var response = httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = response.Content.ReadAsStringAsync().Result;
                var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                return tokenResponse?.ContainsKey("access_token") == true
                    ? tokenResponse["access_token"].ToString()
                    : null;
            }
        }
    }
}
