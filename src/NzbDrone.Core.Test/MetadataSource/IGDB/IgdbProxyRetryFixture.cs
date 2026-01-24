using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource.IGDB;
using NzbDrone.Core.MetadataSource.IGDB.Resource;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MetadataSource.IGDB
{
    [TestFixture]
    public class IgdbProxyRetryFixture : CoreTest<IgdbProxy>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IIgdbAuthService>()
                  .Setup(s => s.GetAccessToken())
                  .Returns("valid-token");

            Mocker.GetMock<IIgdbAuthService>()
                  .Setup(s => s.ClientId)
                  .Returns("test-client-id");
        }

        private HttpResponse CreateHttpResponse(HttpStatusCode statusCode, string content = "[]")
        {
            var request = new HttpRequest("https://api.igdb.com/v4/games");
            var headers = new HttpHeader();
            headers.ContentType = "application/json";
            return new HttpResponse(request, headers, content, statusCode);
        }

        private HttpResponse<List<IgdbGameResource>> CreateTypedResponse(HttpStatusCode statusCode, string content = "[]")
        {
            var response = CreateHttpResponse(statusCode, content);
            return new HttpResponse<List<IgdbGameResource>>(response);
        }

        private HttpResponse<List<IgdbGameResource>> CreateSuccessResponse(int igdbId = 100, string name = "Test Game")
        {
            var json = $"[{{\"id\": {igdbId}, \"name\": \"{name}\", \"slug\": \"test-game\", \"category\": 0}}]";
            return CreateTypedResponse(HttpStatusCode.OK, json);
        }

        [Test]
        public void should_return_game_on_successful_first_request()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(CreateSuccessResponse(100, "Test Game"));

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            result.Title.Should().Be("Test Game");

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Once());
        }

        [Test]
        public void should_retry_on_too_many_requests_status()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount <= 2)
                      {
                          return CreateTypedResponse(HttpStatusCode.TooManyRequests);
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            result.Title.Should().Be("Test Game");
            callCount.Should().Be(3);
        }

        [Test]
        public void should_retry_on_internal_server_error()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount == 1)
                      {
                          return CreateTypedResponse(HttpStatusCode.InternalServerError);
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            callCount.Should().Be(2);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_retry_on_bad_gateway()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount == 1)
                      {
                          return CreateTypedResponse(HttpStatusCode.BadGateway);
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            callCount.Should().Be(2);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_retry_on_service_unavailable()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount == 1)
                      {
                          return CreateTypedResponse(HttpStatusCode.ServiceUnavailable);
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            callCount.Should().Be(2);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_retry_on_gateway_timeout()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount == 1)
                      {
                          return CreateTypedResponse(HttpStatusCode.GatewayTimeout);
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            callCount.Should().Be(2);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_throw_immediately_on_unauthorized()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(CreateTypedResponse(HttpStatusCode.Unauthorized));

            Assert.Throws<HttpException>(() => Subject.GetGameInfo(100));

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_return_empty_on_not_found()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(CreateTypedResponse(HttpStatusCode.NotFound));

            Assert.Throws<GameNotFoundException>(() => Subject.GetGameInfo(100));

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Once());
        }

        [Test]
        public void should_throw_after_max_retries_exhausted_on_retryable_status()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(CreateTypedResponse(HttpStatusCode.ServiceUnavailable));

            Assert.Throws<HttpException>(() => Subject.GetGameInfo(100));

            // MaxRetries = 3, so total attempts = 4 (0, 1, 2, 3)
            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Exactly(4));

            ExceptionVerification.ExpectedWarns(3);
            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_retry_on_http_request_exception()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount == 1)
                      {
                          throw new HttpRequestException("Connection refused");
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            callCount.Should().Be(2);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_retry_on_task_canceled_exception()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount == 1)
                      {
                          throw new TaskCanceledException("Request timed out");
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            callCount.Should().Be(2);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_retry_on_timeout_exception()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount == 1)
                      {
                          throw new TimeoutException("Operation timed out");
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            callCount.Should().Be(2);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_retry_on_non_transient_exception()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Throws(new InvalidOperationException("Non-transient error"));

            Assert.Throws<InvalidOperationException>(() => Subject.GetGameInfo(100));

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Once());
        }

        [Test]
        public void should_throw_transient_exception_after_max_retries_exhausted()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Throws(new HttpRequestException("Persistent connection error"));

            Assert.Throws<HttpRequestException>(() => Subject.GetGameInfo(100));

            // MaxRetries = 3, so 3 retries + the final throw = 4 total attempts
            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Exactly(4));

            ExceptionVerification.ExpectedWarns(3);
        }

        [Test]
        public void should_return_empty_list_when_access_token_is_empty()
        {
            Mocker.GetMock<IIgdbAuthService>()
                  .Setup(s => s.GetAccessToken())
                  .Returns(string.Empty);

            var result = Subject.GetPopularGames();

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Never());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_return_empty_list_when_access_token_is_null()
        {
            Mocker.GetMock<IIgdbAuthService>()
                  .Setup(s => s.GetAccessToken())
                  .Returns((string)null);

            var result = Subject.GetTrendingGames();

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Never());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_set_correct_headers_on_request()
        {
            HttpRequest capturedRequest = null;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Callback<HttpRequest>(r => capturedRequest = r)
                  .Returns(CreateSuccessResponse(100));

            Subject.GetGameInfo(100);

            capturedRequest.Should().NotBeNull();
            capturedRequest.Headers["Client-ID"].Should().Be("test-client-id");
            capturedRequest.Headers["Authorization"].Should().Be("Bearer valid-token");
        }

        [Test]
        public void should_return_empty_for_bulk_info_with_null_ids()
        {
            var result = Subject.GetBulkGameInfo(null);

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_return_empty_for_bulk_info_with_empty_ids()
        {
            var result = Subject.GetBulkGameInfo(new List<int>());

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_recover_and_succeed_after_multiple_transient_failures()
        {
            var callCount = 0;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(() =>
                  {
                      callCount++;
                      if (callCount <= 3)
                      {
                          return CreateTypedResponse(HttpStatusCode.TooManyRequests);
                      }

                      return CreateSuccessResponse(100);
                  });

            var result = Subject.GetGameInfo(100);

            result.Should().NotBeNull();
            result.Title.Should().Be("Test Game");
            callCount.Should().Be(4);

            ExceptionVerification.ExpectedWarns(3);
        }

        [Test]
        public void should_not_retry_non_retryable_http_status_codes()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(CreateTypedResponse(HttpStatusCode.BadRequest));

            Assert.Throws<HttpException>(() => Subject.GetGameInfo(100));

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_throw_game_not_found_when_response_is_empty_list()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()))
                  .Returns(CreateTypedResponse(HttpStatusCode.OK, "[]"));

            Assert.Throws<GameNotFoundException>(() => Subject.GetGameInfo(100));
        }

        [Test]
        public void should_return_null_for_steam_app_id_lookup()
        {
            var result = Subject.GetGameInfoBySteamAppId(12345);

            result.Should().BeNull();

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Post<List<IgdbGameResource>>(It.IsAny<HttpRequest>()), Times.Never());
        }
    }
}
