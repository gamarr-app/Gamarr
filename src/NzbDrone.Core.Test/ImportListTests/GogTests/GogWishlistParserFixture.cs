using System.Net;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.GogWishlist;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.GogTests
{
    [TestFixture]
    public class GogWishlistParserFixture : CoreTest<GogWishlistParser>
    {
        private ImportListResponse GivenResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK, int page = 1)
        {
            var request = new ImportListRequest($"https://www.gog.com/u/chandra/wishlist?page={page}", HttpAccept.Html);
            var response = new HttpResponse(request.HttpRequest, new HttpHeader(), content, statusCode);

            return new ImportListResponse(request, response);
        }

        [Test]
        public void should_parse_games_from_wishlist_page()
        {
            var response = GivenResponse(ReadAllText("Files/gog_wishlist.html"));

            var products = Subject.ParseProducts(response);

            products.Should().HaveCount(3);

            products[0].GogId.Should().Be(1456460669);
            products[0].Title.Should().Be("Baldur's Gate 3");
            products[0].Year.Should().Be(2023);

            products[1].GogId.Should().Be(2034949552);
            products[1].Title.Should().Be("The End of the Sun");
            products[1].Year.Should().Be(0);

            products[2].GogId.Should().Be(1207666633);
            products[2].Title.Should().Be("Upcoming Game");
            products[2].Year.Should().Be(2026);
        }

        [Test]
        public void should_exclude_non_game_products()
        {
            var response = GivenResponse(ReadAllText("Files/gog_wishlist.html"));

            var products = Subject.ParseProducts(response);

            products.Should().NotContain(p => p.Title == "Some Documentary");
        }

        [Test]
        public void should_map_products_to_import_list_games()
        {
            var response = GivenResponse(ReadAllText("Files/gog_wishlist.html"));

            var games = Subject.ParseResponse(response);

            games.Should().HaveCount(3);
            games[0].Title.Should().Be("Baldur's Gate 3");
            games[0].Year.Should().Be(2023);
        }

        [Test]
        public void should_throw_on_not_found()
        {
            var response = GivenResponse("not found", HttpStatusCode.NotFound);

            Assert.Throws<ImportListException>(() => Subject.ParseProducts(response))
                .Message.Should().Contain("public");
        }

        [Test]
        public void should_throw_on_unexpected_status_code()
        {
            var response = GivenResponse("oops", HttpStatusCode.InternalServerError);

            Assert.Throws<ImportListException>(() => Subject.ParseProducts(response));
        }

        [Test]
        public void should_throw_when_page_has_no_gog_data()
        {
            var response = GivenResponse("<html><body>No data here</body></html>");

            Assert.Throws<ImportListException>(() => Subject.ParseProducts(response));
        }

        [Test]
        public void should_return_empty_when_gog_clamps_page_number()
        {
            // Requesting page 2 of a one-page wishlist returns page 1 again
            var response = GivenResponse(ReadAllText("Files/gog_wishlist.html"), page: 2);

            Subject.ParseProducts(response).Should().BeEmpty();
        }

        [Test]
        public void should_extract_gog_data_with_braces_inside_strings()
        {
            var html = "var gogData = {\"page\":1,\"products\":[{\"id\":1,\"title\":\"a } b { c\",\"description\":\"x\\\"}\"}]};\n gogData.features = {};";

            var json = GogWishlistParser.ExtractGogData(html);

            json.Should().StartWith("{\"page\":1");
            json.Should().EndWith("}]}");
        }

        [Test]
        public void should_return_null_when_extracting_from_html_without_marker()
        {
            GogWishlistParser.ExtractGogData("<html></html>").Should().BeNull();
        }
    }
}
