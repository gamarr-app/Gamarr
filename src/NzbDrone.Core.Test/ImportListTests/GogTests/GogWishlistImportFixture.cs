using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Gog;
using NzbDrone.Core.ImportLists.GogWishlist;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ImportListTests.GogTests
{
    [TestFixture]
    public class GogWishlistImportFixture : CoreTest<GogWishlistImport>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new ImportListDefinition
            {
                Id = 1,
                Name = "GOG Wishlist",
                EnableAuto = true,
                Settings = new GogWishlistSettings { Username = "chandra" }
            };
        }

        private void GivenResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(o => o.Execute(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), content, statusCode));
        }

        private void GivenPassThroughResolver()
        {
            Mocker.GetMock<IGogGameResolver>()
                  .Setup(s => s.ResolveGames(It.IsAny<IList<GogProduct>>()))
                  .Returns<IList<GogProduct>>(products => products
                      .Select(p => new ImportListGame
                      {
                          Title = p.Title,
                          Year = p.Year,
                          IgdbId = (int)(p.GogId % 100000)
                      })
                      .ToList());
        }

        [Test]
        public void should_fetch_and_resolve_wishlist()
        {
            GivenResponse(ReadAllText("Files/gog_wishlist.html"));
            GivenPassThroughResolver();

            var result = Subject.Fetch();

            result.AnyFailure.Should().BeFalse();
            result.Games.Should().HaveCount(3);
            result.Games.Should().OnlyContain(g => g.IgdbId > 0);

            // 3 items < page size of 100, so paging must stop after the first request
            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Execute(It.IsAny<HttpRequest>()), Times.Once());
        }

        [Test]
        public void should_record_failure_and_return_empty_for_missing_or_private_profile()
        {
            GivenResponse("not found", HttpStatusCode.NotFound);

            var result = Subject.Fetch();

            result.AnyFailure.Should().BeTrue();
            result.Games.Should().BeEmpty();

            Mocker.GetMock<IImportListStatusService>()
                  .Verify(v => v.RecordFailure(1, default), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_filter_out_unresolved_games()
        {
            GivenResponse(ReadAllText("Files/gog_wishlist.html"));

            // Resolver couldn't map anything: items come back without ids
            Mocker.GetMock<IGogGameResolver>()
                  .Setup(s => s.ResolveGames(It.IsAny<IList<GogProduct>>()))
                  .Returns<IList<GogProduct>>(products => products
                      .Select(p => new ImportListGame { Title = p.Title, Year = p.Year })
                      .ToList());

            var result = Subject.Fetch();

            result.AnyFailure.Should().BeFalse();
            result.Games.Should().BeEmpty();
        }
    }
}
