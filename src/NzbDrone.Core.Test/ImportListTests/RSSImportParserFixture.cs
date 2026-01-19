using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.RSSImport;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportList
{
    public class RSSImportTest : CoreTest<RSSImportParser>
    {
        private ImportListResponse CreateResponse(string url, string content)
        {
            var httpRequest = new HttpRequest(url);
            var httpResponse = new HttpResponse(httpRequest, new HttpHeader(), Encoding.UTF8.GetBytes(content));

            return new ImportListResponse(new ImportListRequest(httpRequest), httpResponse);
        }

        [Test]
        public void should_parse_rss_xml()
        {
            var xml = ReadAllText("Files/rss_games.xml");

            var result = Subject.ParseResponse(CreateResponse("http://my.indexer.com/api?q=My+Favourite+Game", xml));

            result.Should().NotBeEmpty();
        }
    }
}
