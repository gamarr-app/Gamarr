using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists.SteamWishlist;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.SteamWishlistTests
{
    [TestFixture]
    public class SteamWishlistRequestGeneratorFixture : CoreTest<SteamWishlistRequestGenerator>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Settings = new SteamWishlistSettings { SteamUserId = "chandra" };
            Subject.HttpClient = Mocker.GetMock<IHttpClient>().Object;
            Subject.Logger = TestLogger;
        }

        private void GivenProfileXml(string content)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(o => o.Get(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), content, HttpStatusCode.OK));
        }

        [Test]
        public void should_use_numeric_id_without_resolving()
        {
            Subject.Settings = new SteamWishlistSettings { SteamUserId = "76561198000000000" };

            Subject.GetGames().GetAllTiers().Single().Single().Url.FullUri.Should().Contain("steamid=76561198000000000");

            Mocker.GetMock<IHttpClient>().Verify(o => o.Get(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_resolve_vanity_url_to_steam_id()
        {
            GivenProfileXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><profile><steamID64>76561198000000001</steamID64></profile>");

            Subject.GetGames().GetAllTiers().Single().Single().Url.FullUri.Should().Contain("steamid=76561198000000001");
        }

        [Test]
        public void should_resolve_vanity_url_when_profile_xml_is_malformed()
        {
            // Steam does not escape entities in user-supplied fields, which makes the document invalid XML.
            GivenProfileXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><profile><steamID64>76561198000000002</steamID64><summary>Tom &amp Jerry &nbsp; R&D</summary></profile>");

            Subject.GetGames().GetAllTiers().Single().Single().Url.FullUri.Should().Contain("steamid=76561198000000002");
        }

        [Test]
        public void should_throw_when_steam_id_cannot_be_resolved()
        {
            GivenProfileXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><response><error>The specified profile could not be found.</error></response>");

            Assert.Throws<System.Exception>(() => Subject.GetGames());
        }
    }
}
