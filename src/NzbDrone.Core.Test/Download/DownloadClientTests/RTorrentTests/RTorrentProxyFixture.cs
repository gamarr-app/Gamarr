using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download.Clients.RTorrent;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.RTorrentTests
{
    [TestFixture]
    public class RTorrentProxyFixture : CoreTest<RTorrentProxy>
    {
        private RTorrentSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new RTorrentSettings
            {
                Host = "localhost",
                Port = 8080,
                UrlBase = "RPC2"
            };
        }

        private void GivenXmlRpcResponse(string value)
        {
            var xmlResponse = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<methodResponse><params><param><value><string>" +
                value +
                "</string></value></param></params></methodResponse>";

            Mocker.GetMock<IHttpClient>()
                .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), xmlResponse));
        }

        private void GivenHttpThrows()
        {
            Mocker.GetMock<IHttpClient>()
                .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
                .Throws(new WebException("connection refused"));
        }

        [Test]
        public void should_return_true_when_torrent_exists()
        {
            GivenXmlRpcResponse("My.Game.torrent");

            var result = Subject.HasHashTorrent("ABC123", _settings);

            result.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_name_is_empty()
        {
            GivenXmlRpcResponse("");

            var result = Subject.HasHashTorrent("ABC123", _settings);

            result.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_torrent_is_meta()
        {
            var hash = "ABC123";
            GivenXmlRpcResponse(hash + ".meta");

            var result = Subject.HasHashTorrent(hash, _settings);

            result.Should().BeFalse();
        }

        [Test]
        public void should_return_false_on_exception()
        {
            GivenHttpThrows();

            var result = Subject.HasHashTorrent("ABC123", _settings);

            result.Should().BeFalse();
        }
    }
}
