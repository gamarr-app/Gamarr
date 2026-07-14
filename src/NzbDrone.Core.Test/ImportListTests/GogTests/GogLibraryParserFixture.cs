using System.Net;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.GogLibrary;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.GogTests
{
    [TestFixture]
    public class GogLibraryParserFixture : CoreTest<GogLibraryParser>
    {
        private ImportListResponse GivenResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK, int page = 1)
        {
            var request = new ImportListRequest($"https://www.gog.com/u/chandra/games/stats?page={page}", HttpAccept.Json);
            var response = new HttpResponse(request.HttpRequest, new HttpHeader(), content, statusCode);

            return new ImportListResponse(request, response);
        }

        [Test]
        public void should_parse_owned_games()
        {
            var response = GivenResponse(ReadAllText("Files/gog_library.json"));

            var products = Subject.ParseProducts(response);

            products.Should().HaveCount(3);

            products[0].GogId.Should().Be(1207187357);
            products[0].Title.Should().Be("Pathfinder: Wrath of the Righteous");
            products[0].Year.Should().Be(0);

            products[1].GogId.Should().Be(1423049311);
            products[1].Title.Should().Be("Cyberpunk 2077");

            // Delisted title ("game": null) becomes an empty placeholder so the
            // page-fullness check still sees the server's true page size.
            products[2].GogId.Should().Be(0);
            products[2].Title.Should().BeNull();
        }

        [Test]
        public void should_map_products_to_import_list_games()
        {
            var response = GivenResponse(ReadAllText("Files/gog_library.json"));

            var games = Subject.ParseResponse(response);

            games.Should().HaveCount(3);
            games[0].Title.Should().Be("Pathfinder: Wrath of the Righteous");
        }

        [Test]
        public void should_throw_with_private_message_on_forbidden()
        {
            var response = GivenResponse("denied", HttpStatusCode.Forbidden);

            Assert.Throws<ImportListException>(() => Subject.ParseProducts(response))
                .Message.Should().Contain("private");
        }

        [Test]
        public void should_throw_on_not_found()
        {
            var response = GivenResponse("not found", HttpStatusCode.NotFound);

            Assert.Throws<ImportListException>(() => Subject.ParseProducts(response));
        }

        [Test]
        public void should_throw_on_unexpected_status_code()
        {
            var response = GivenResponse("oops", HttpStatusCode.BadGateway);

            Assert.Throws<ImportListException>(() => Subject.ParseProducts(response));
        }

        [Test]
        public void should_return_empty_when_gog_clamps_page_number()
        {
            var response = GivenResponse(ReadAllText("Files/gog_library.json"), page: 2);

            Subject.ParseProducts(response).Should().BeEmpty();
        }

        [Test]
        public void should_return_empty_for_empty_items()
        {
            var response = GivenResponse("{\"page\":1,\"pages\":0,\"total\":0,\"_embedded\":{\"items\":[]}}");

            Subject.ParseProducts(response).Should().BeEmpty();
        }
    }
}
