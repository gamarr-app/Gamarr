using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Http
{
    [TestFixture]
    public class ManagedHttpDispatcherFixture : TestBase
    {
        private TestDispatcher Build(Exception toThrow)
        {
            var client = new System.Net.Http.HttpClient(new ThrowingHandler(toThrow));

            var userAgentBuilder = new Mock<IUserAgentBuilder>();
            userAgentBuilder.Setup(s => s.GetUserAgent(It.IsAny<bool>()))
                            .Returns("Gamarr/Test");

            return new TestDispatcher(client,
                new Mock<IHttpProxySettingsProvider>().Object,
                new Mock<ICreateManagedWebProxy>().Object,
                new Mock<ICertificateValidationService>().Object,
                userAgentBuilder.Object,
                new CacheManager(),
                TestLogger);
        }

        [Test]
        public void should_normalize_socket_exception_to_http_request_exception()
        {
            // A bare SocketException escapes SocketsHttpHandler on some paths - notably the connection
            // pool's RemoteEndPoint lookup on the Stream our ConnectCallback returns. Callers only handle
            // HttpRequestException/WebException, so it must not surface raw.
            var inner = new SocketException((int)SocketError.NotConnected);

            var subject = Build(inner);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                subject.GetResponseAsync(new HttpRequest("https://localhost/get"), new CookieContainer()));

            ex.InnerException.Should().BeSameAs(inner);
            ex.HttpRequestError.Should().Be(HttpRequestError.ConnectionError);
        }

        [Test]
        public void should_not_rewrap_http_request_exception()
        {
            var inner = new HttpRequestException("already normalized");

            var subject = Build(inner);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                subject.GetResponseAsync(new HttpRequest("https://localhost/get"), new CookieContainer()));

            ex.Should().BeSameAs(inner);
        }

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            private readonly Exception _exception;

            public ThrowingHandler(Exception exception)
            {
                _exception = exception;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw _exception;
            }
        }

        private sealed class TestDispatcher : ManagedHttpDispatcher
        {
            private readonly System.Net.Http.HttpClient _client;

            public TestDispatcher(System.Net.Http.HttpClient client,
                IHttpProxySettingsProvider proxySettingsProvider,
                ICreateManagedWebProxy createManagedWebProxy,
                ICertificateValidationService certificateValidationService,
                IUserAgentBuilder userAgentBuilder,
                ICacheManager cacheManager,
                Logger logger)
                : base(proxySettingsProvider, createManagedWebProxy, certificateValidationService, userAgentBuilder, cacheManager, logger)
            {
                _client = client;
            }

            protected override System.Net.Http.HttpClient GetClient(HttpUri uri)
            {
                return _client;
            }
        }
    }
}
