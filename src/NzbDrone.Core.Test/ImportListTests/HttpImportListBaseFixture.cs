using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.RSSImport;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ImportListTests
{
    [TestFixture]
    public class HttpImportListBaseFixture : CoreTest<TestImportList>
    {
        private const string _listUrl = "http://my.list.tv/games.rss";

        [SetUp]
        public void Setup()
        {
            var chain = new ImportListPageableRequestChain();
            chain.Add(new[] { new ImportListRequest(_listUrl, HttpAccept.Rss) });

            Subject._requestGenerator = Mocker.GetMock<IImportListRequestGenerator>().Object;
            Mocker.GetMock<IImportListRequestGenerator>()
                  .Setup(v => v.GetGames())
                  .Returns(chain);

            Subject._parser = new RSSImportParser(new RSSImportSettings(), TestLogger);

            Subject.Definition = new ImportListDefinition
            {
                Name = "Test Import List",
                Settings = new TestImportListSettings()
            };
        }

        private void GivenResponse(string content)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(o => o.Execute(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), content));
        }

        [Test]
        public void should_not_throw_and_should_warn_when_feed_is_html_not_xml()
        {
            GivenResponse("<html><body>Site is down for maintenance<br>Please try again later.</body></html>");

            var result = Subject.Fetch();

            result.Games.Should().BeEmpty();
            result.AnyFailure.Should().BeTrue();

            Mocker.GetMock<IImportListStatusService>()
                  .Verify(v => v.RecordFailure(It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
