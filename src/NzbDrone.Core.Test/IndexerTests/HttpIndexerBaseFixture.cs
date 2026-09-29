using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class HttpIndexerBaseFixture : CoreTest<TestIndexer>
    {
        private const string _indexerUrl = "http://my.indexer.tv/recent";

        [SetUp]
        public void Setup()
        {
            var chain = new IndexerPageableRequestChain();
            chain.Add(new[] { new IndexerRequest(_indexerUrl, HttpAccept.Rss) });

            Subject._requestGenerator = Mocker.GetMock<IIndexerRequestGenerator>().Object;
            Mocker.GetMock<IIndexerRequestGenerator>()
                  .Setup(v => v.GetRecentRequests())
                  .Returns(chain);

            Subject._parser = new RssParser();

            Subject.Definition = new IndexerDefinition
            {
                Name = "Test Indexer",
                Settings = new TestIndexerSettings()
            };
        }

        private void GivenResponse(string content)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(o => o.ExecuteAsync(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), content)));
        }

        [Test]
        public async Task should_not_throw_and_should_warn_when_feed_is_html_not_xml()
        {
            GivenResponse("<html><body>Site is down for maintenance<br>Please try again later.</body></html>");

            var releases = await Subject.FetchRecent();

            releases.Should().BeEmpty();

            Mocker.GetMock<IIndexerStatusService>()
                  .Verify(v => v.RecordFailure(It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
