using System;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Download.Clients.DownloadStation;
using NzbDrone.Core.Download.Clients.DownloadStation.Proxies;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.DownloadStationTests
{
    [TestFixture]
    public class DiskStationSessionRetryFixture : CoreTest<DSMInfoProxy>
    {
        private const string ApiInfoJson = "{\"success\":true,\"data\":{" +
            "\"SYNO.API.Auth\":{\"path\":\"auth.cgi\",\"minVersion\":1,\"maxVersion\":6}," +
            "\"SYNO.DSM.Info\":{\"path\":\"entry.cgi\",\"minVersion\":1,\"maxVersion\":2}}}";

        private const string ExpiredSessionJson = "{\"success\":false,\"error\":{\"code\":119}}";
        private const string SerialJson = "{\"success\":true,\"data\":{\"serial\":\"ABC123\"}}";

        private DownloadStationSettings _settings;
        private int _logins;
        private int _infoCalls;
        private bool _expireEverySession;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());

            _settings = new DownloadStationSettings
            {
                Host = "127.0.0.1",
                Port = 5000,
                Username = "admin",
                Password = "pass"
            };

            _logins = 0;
            _infoCalls = 0;
            _expireEverySession = false;

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(Respond);
        }

        private HttpResponse Respond(HttpRequest request)
        {
            var url = request.Url.FullUri;
            string content;

            if (url.Contains("method=query"))
            {
                content = ApiInfoJson;
            }
            else if (url.Contains("method=login"))
            {
                _logins++;
                content = $"{{\"success\":true,\"data\":{{\"sid\":\"sid{_logins}\"}}}}";
            }
            else if (url.Contains("method=getinfo"))
            {
                _infoCalls++;

                // The first session handed out has expired on the DSM side.
                content = _expireEverySession || url.Contains("_sid=sid1") ? ExpiredSessionJson : SerialJson;
            }
            else
            {
                throw new InvalidOperationException("Unexpected request " + url);
            }

            return new HttpResponse(request, new HttpHeader(), content, HttpStatusCode.OK);
        }

        [Test]
        public void should_sign_in_again_and_retry_when_session_expired()
        {
            Subject.GetSerialNumber(_settings).Should().Be("ABC123");

            _logins.Should().Be(2);
            _infoCalls.Should().Be(2);
        }

        [Test]
        public void should_retry_only_once_and_report_client_unavailable()
        {
            _expireEverySession = true;

            Assert.Throws<DownloadClientUnavailableException>(() => Subject.GetSerialNumber(_settings));

            _logins.Should().Be(2);
            _infoCalls.Should().Be(2);

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
