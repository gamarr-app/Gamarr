using System.Net.Http;
using System.Net.Sockets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.SteamWishlist;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ImportListTests.SteamWishlistTests
{
    [TestFixture]
    public class SteamWishlistImportFixture : CoreTest<SteamWishlistImport>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new ImportListDefinition
            {
                Id = 1,
                Name = "Steam Wishlist",
                EnableAuto = true,

                // Numeric, so the request generator does not resolve a vanity URL first
                Settings = new SteamWishlistSettings { SteamUserId = "76561198000000000" }
            };
        }

        private void GivenTransportFailure(HttpRequestException exception)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(o => o.Execute(It.IsAny<HttpRequest>()))
                  .Throws(exception);
        }

        // The dispatcher converts timeouts and truncated reads into WebException, but a
        // failed TLS handshake stays an HttpRequestException all the way up. That used to
        // reach the catch-all in HttpImportListBase and log at Error, which is how a
        // single reset by Steam's own server arrived in Sentry as a crash report.
        [Test]
        public void should_warn_and_record_connection_failure_when_tls_handshake_fails()
        {
            GivenTransportFailure(new HttpRequestException(
                HttpRequestError.SecureConnectionError,
                "The SSL connection could not be established, see inner exception.",
                new SocketException((int)SocketError.ConnectionReset)));

            var result = Subject.Fetch();

            result.AnyFailure.Should().BeTrue();
            result.Games.Should().BeEmpty();

            Mocker.GetMock<IImportListStatusService>()
                  .Verify(v => v.RecordConnectionFailure(1), Times.Once());

            // ExceptionVerification fails the test on any unexpected Error, which is the
            // regression this guards: warn, do not report.
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_record_connection_failure_when_dns_fails()
        {
            GivenTransportFailure(new HttpRequestException(
                HttpRequestError.NameResolutionError,
                "No such host is known."));

            Subject.Fetch().AnyFailure.Should().BeTrue();

            Mocker.GetMock<IImportListStatusService>()
                  .Verify(v => v.RecordConnectionFailure(1), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        // Not every HttpRequestException is the network being unreachable, and only the
        // transport ones should back the list off as a connection failure.
        [Test]
        public void should_record_plain_failure_for_a_non_transport_http_error()
        {
            GivenTransportFailure(new HttpRequestException(
                HttpRequestError.InvalidResponse,
                "The response ended prematurely."));

            Subject.Fetch().AnyFailure.Should().BeTrue();

            Mocker.GetMock<IImportListStatusService>()
                  .Verify(v => v.RecordFailure(1, default), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
