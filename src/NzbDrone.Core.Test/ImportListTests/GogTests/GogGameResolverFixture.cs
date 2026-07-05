using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.ImportLists.Gog;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ImportListTests.GogTests
{
    [TestFixture]
    public class GogGameResolverFixture : CoreTest<GogGameResolver>
    {
        private void GivenIgdbCredentials(bool configured)
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.IgdbClientId)
                  .Returns(configured ? "clientId" : string.Empty);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.IgdbClientSecret)
                  .Returns(configured ? "clientSecret" : string.Empty);
        }

        private static GogProduct Product(long gogId, string title, int year = 0)
        {
            return new GogProduct { GogId = gogId, Title = title, Year = year };
        }

        private static Game SearchResult(string title, int year, int igdbId = 0, int steamAppId = 0)
        {
            return new Game
            {
                Title = title,
                Year = year,
                IgdbId = igdbId,
                SteamAppId = steamAppId
            };
        }

        [Test]
        public void should_resolve_via_igdb_external_ids_when_igdb_configured()
        {
            GivenIgdbCredentials(true);

            Mocker.GetMock<IProvideExternalGameIdMapping>()
                  .Setup(s => s.GetIgdbIdsByGogIds(It.IsAny<ICollection<long>>()))
                  .Returns(new Dictionary<long, int> { { 1456460669, 119171 } });

            var result = Subject.ResolveGames(new List<GogProduct> { Product(1456460669, "Baldur's Gate 3", 2023) });

            result.Should().HaveCount(1);
            result[0].IgdbId.Should().Be(119171);
            result[0].Title.Should().Be("Baldur's Gate 3");
            result[0].Year.Should().Be(2023);

            Mocker.GetMock<ISearchForNewGame>()
                  .Verify(v => v.SearchForNewGame(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_call_igdb_mapping_when_igdb_not_configured()
        {
            GivenIgdbCredentials(false);

            Mocker.GetMock<ISearchForNewGame>()
                  .Setup(s => s.SearchForNewGame(It.IsAny<string>()))
                  .Returns(new List<Game>());

            Subject.ResolveGames(new List<GogProduct> { Product(1, "Some Game") });

            Mocker.GetMock<IProvideExternalGameIdMapping>()
                  .Verify(v => v.GetIgdbIdsByGogIds(It.IsAny<ICollection<long>>()), Times.Never());
        }

        [Test]
        public void should_fall_back_to_title_search_for_unmapped_products()
        {
            GivenIgdbCredentials(true);

            Mocker.GetMock<IProvideExternalGameIdMapping>()
                  .Setup(s => s.GetIgdbIdsByGogIds(It.IsAny<ICollection<long>>()))
                  .Returns(new Dictionary<long, int>());

            Mocker.GetMock<ISearchForNewGame>()
                  .Setup(s => s.SearchForNewGame("Cyberpunk 2077"))
                  .Returns(new List<Game> { SearchResult("Cyberpunk 2077", 2020, igdbId: 1877, steamAppId: 1091500) });

            var result = Subject.ResolveGames(new List<GogProduct> { Product(1423049311, "Cyberpunk 2077", 2020) });

            result.Should().HaveCount(1);
            result[0].IgdbId.Should().Be(1877);
            result[0].SteamAppId.Should().Be(1091500);
        }

        [Test]
        public void should_match_title_ignoring_case_and_punctuation()
        {
            GivenIgdbCredentials(false);

            Mocker.GetMock<ISearchForNewGame>()
                  .Setup(s => s.SearchForNewGame(It.IsAny<string>()))
                  .Returns(new List<Game> { SearchResult("Baldur's Gate 3", 2023, igdbId: 119171) });

            var result = Subject.ResolveGames(new List<GogProduct> { Product(1, "Baldurs Gate 3", 2023) });

            result[0].IgdbId.Should().Be(119171);
        }

        [Test]
        public void should_prefer_candidate_with_matching_year()
        {
            GivenIgdbCredentials(false);

            Mocker.GetMock<ISearchForNewGame>()
                  .Setup(s => s.SearchForNewGame(It.IsAny<string>()))
                  .Returns(new List<Game>
                  {
                      SearchResult("Doom", 1993, igdbId: 1),
                      SearchResult("Doom", 2016, igdbId: 2)
                  });

            var result = Subject.ResolveGames(new List<GogProduct> { Product(1, "DOOM", 2016) });

            result[0].IgdbId.Should().Be(2);
        }

        [Test]
        public void should_not_match_when_titles_differ()
        {
            GivenIgdbCredentials(false);

            Mocker.GetMock<ISearchForNewGame>()
                  .Setup(s => s.SearchForNewGame(It.IsAny<string>()))
                  .Returns(new List<Game> { SearchResult("A Completely Different Game", 2020, igdbId: 42) });

            var result = Subject.ResolveGames(new List<GogProduct> { Product(1, "Some Obscure Game", 2020) });

            result.Should().HaveCount(1);
            result[0].IgdbId.Should().Be(0);
            result[0].SteamAppId.Should().Be(0);
        }

        [Test]
        public void should_fall_back_to_title_search_when_igdb_mapping_throws()
        {
            GivenIgdbCredentials(true);

            Mocker.GetMock<IProvideExternalGameIdMapping>()
                  .Setup(s => s.GetIgdbIdsByGogIds(It.IsAny<ICollection<long>>()))
                  .Throws(new Exception("igdb down"));

            Mocker.GetMock<ISearchForNewGame>()
                  .Setup(s => s.SearchForNewGame("The Witcher 3"))
                  .Returns(new List<Game> { SearchResult("The Witcher 3", 2015, igdbId: 1942) });

            var result = Subject.ResolveGames(new List<GogProduct> { Product(1207664663, "The Witcher 3", 2015) });

            result[0].IgdbId.Should().Be(1942);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_fail_when_search_throws()
        {
            GivenIgdbCredentials(false);

            Mocker.GetMock<ISearchForNewGame>()
                  .Setup(s => s.SearchForNewGame(It.IsAny<string>()))
                  .Throws(new Exception("search down"));

            var result = Subject.ResolveGames(new List<GogProduct> { Product(1, "Some Game") });

            result.Should().HaveCount(1);
            result[0].IgdbId.Should().Be(0);
        }

        [Test]
        public void should_take_year_from_match_when_product_has_no_year()
        {
            GivenIgdbCredentials(false);

            Mocker.GetMock<ISearchForNewGame>()
                  .Setup(s => s.SearchForNewGame(It.IsAny<string>()))
                  .Returns(new List<Game> { SearchResult("The End of the Sun", 2024, igdbId: 55555) });

            var result = Subject.ResolveGames(new List<GogProduct> { Product(2034949552, "The End of the Sun") });

            result[0].IgdbId.Should().Be(55555);
            result[0].Year.Should().Be(2024);
        }

        [Test]
        public void should_return_empty_for_empty_input()
        {
            Subject.ResolveGames(new List<GogProduct>()).Should().BeEmpty();
            Subject.ResolveGames(null).Should().BeEmpty();
        }
    }
}
