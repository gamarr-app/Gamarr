using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource.IGDB;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.IGDB
{
    [TestFixture]
    public class IgdbAuthServiceFixture : CoreTest<IgdbAuthService>
    {
        private Mock<ICached<IgdbToken>> _cacheMock;

        [SetUp]
        public void Setup()
        {
            _cacheMock = new Mock<ICached<IgdbToken>>();

            Mocker.GetMock<ICacheManager>()
                .Setup(s => s.GetCache<IgdbToken>(It.IsAny<Type>()))
                .Returns(_cacheMock.Object);
        }

        [Test]
        public void should_detect_token_expiry()
        {
            var token = new IgdbToken
            {
                AccessToken = "test_token",
                ExpiresIn = 3600,
                ObtainedAt = DateTime.UtcNow.AddSeconds(-3600)
            };

            token.IsExpired.Should().BeTrue();
        }

        [Test]
        public void should_not_be_expired_when_within_validity()
        {
            var token = new IgdbToken
            {
                AccessToken = "test_token",
                ExpiresIn = 7200,
                ObtainedAt = DateTime.UtcNow
            };

            token.IsExpired.Should().BeFalse();
        }

        [Test]
        public void should_return_null_when_no_credentials()
        {
            _cacheMock.Setup(s => s.Find("igdb_token"))
                .Returns((IgdbToken)null);

            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.IgdbClientId)
                .Returns(string.Empty);

            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.IgdbClientSecret)
                .Returns(string.Empty);

            Subject.GetAccessToken().Should().BeNull();
        }

        [Test]
        public void should_return_cached_token_when_not_expired()
        {
            var cachedToken = new IgdbToken
            {
                AccessToken = "cached_token",
                ExpiresIn = 7200,
                ObtainedAt = DateTime.UtcNow
            };

            _cacheMock.Setup(s => s.Find("igdb_token"))
                .Returns(cachedToken);

            Subject.GetAccessToken().Should().Be("cached_token");
        }

        [Test]
        public void should_fetch_new_token_when_cache_empty()
        {
            _cacheMock.Setup(s => s.Find("igdb_token"))
                .Returns((IgdbToken)null);

            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.IgdbClientId)
                .Returns("test_client_id");

            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.IgdbClientSecret)
                .Returns("test_client_secret");

            var httpResponse = new HttpResponse(new HttpRequest("https://id.twitch.tv/oauth2/token"), new HttpHeader(), "{\"access_token\":\"new_token\",\"expires_in\":7200,\"token_type\":\"bearer\"}");

            Mocker.GetMock<IHttpClient>()
                .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
                .Returns(httpResponse);

            var result = Subject.GetAccessToken();

            result.Should().Be("new_token");
            _cacheMock.Verify(s => s.Set("igdb_token", It.Is<IgdbToken>(t => t.AccessToken == "new_token"), It.IsAny<TimeSpan>()), Times.Once());
        }
    }
}
