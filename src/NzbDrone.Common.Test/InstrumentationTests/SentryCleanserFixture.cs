using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Instrumentation.Sentry;
using NzbDrone.Test.Common;
using Sentry;
using Sentry.Protocol;

namespace NzbDrone.Common.Test.InstrumentationTests
{
    [TestFixture]
    public class SentryCleanserFixture : TestBase
    {
        private static SentryEvent EventWith(string logger, string exceptionType, string exceptionMessage)
        {
            var e = new SentryEvent { Logger = logger };
            var ex = new SentryException
            {
                Type = exceptionType,
                Value = exceptionMessage,
                Stacktrace = new SentryStackTrace()
            };
            e.SentryExceptions = new[] { ex };
            return e;
        }

        [TestCase("RawgProxy", "HTTP request failed: [502:BadGateway] [GET] at [https://api.rawg.io/api/games?key=x&search=y]")]
        [TestCase("IgdbProxy", "HTTP request failed: [503:ServiceUnavailable] [POST] at [https://api.igdb.com/v4/games]")]
        [TestCase("SteamStoreProxy", "HTTP request failed: [504:GatewayTimeout] [GET] at [https://store.steampowered.com/api/appdetails]")]
        [TestCase("NzbDrone.Core.MetadataSource.IGDB.IgdbProxy", "HTTP request failed: [500:InternalServerError] [GET] at [...]")]
        public void should_drop_5xx_from_external_metadata_proxies(string logger, string message)
        {
            var e = EventWith(logger, "NzbDrone.Common.Http.HttpException", message);
            SentryCleanser.CleanseEvent(e).Should().BeNull();
        }

        [Test]
        public void should_keep_4xx_from_external_metadata_proxies()
        {
            var e = EventWith(
                "RawgProxy",
                "NzbDrone.Common.Http.HttpException",
                "HTTP request failed: [401:Unauthorized] [GET] at [https://api.rawg.io/api/games?key=bad]");
            SentryCleanser.CleanseEvent(e).Should().NotBeNull();
        }

        [Test]
        public void should_keep_5xx_from_unrelated_loggers()
        {
            var e = EventWith(
                "ImportListService",
                "NzbDrone.Common.Http.HttpException",
                "HTTP request failed: [502:BadGateway] [GET] at [https://something-else]");
            SentryCleanser.CleanseEvent(e).Should().NotBeNull();
        }

        [Test]
        public void should_keep_non_http_exceptions_from_proxy_loggers()
        {
            var e = EventWith("RawgProxy", "System.InvalidOperationException", "Sequence contains no matching element");
            SentryCleanser.CleanseEvent(e).Should().NotBeNull();
        }
    }
}
