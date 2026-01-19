#pragma warning disable CS0618
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    public class NewznabRequestGeneratorFixture : CoreTest<NewznabRequestGenerator>
    {
        private GameSearchCriteria _gameSearchCriteria;
        private NewznabCapabilities _capabilities;

        [SetUp]
        public void SetUp()
        {
            Subject.Settings = new NewznabSettings()
            {
                BaseUrl = "http://127.0.0.1:1234/",
                Categories = new[] { 1, 2 },
                ApiKey = "abcd",
            };

            _gameSearchCriteria = new GameSearchCriteria
            {
                Game = new Games.Game { Title = "Star Wars", Year = 1977, IgdbId = 11 },
                SceneTitles = new List<string> { "Star Wars" }
            };

            _capabilities = new NewznabCapabilities();

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>()))
                .Returns(_capabilities);
        }

        [Test]
        public void should_use_all_categories_for_feed()
        {
            var results = Subject.GetRecentRequests();

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("&cat=1,2&");
        }

        [Test]
        public void should_not_have_duplicate_categories()
        {
            Subject.Settings.Categories = new[] { 1, 2, 2, 3 };

            var results = Subject.GetRecentRequests();

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.FullUri.Should().Contain("&cat=1,2,3&");
        }

        [Test]
        public void should_return_subsequent_pages()
        {
            var results = Subject.GetSearchRequests(_gameSearchCriteria);

            results.GetAllTiers().Should().HaveCount(2);

            var pages = results.GetAllTiers().First().Take(3).ToList();

            pages[0].Url.FullUri.Should().Contain("&offset=0&");
            pages[1].Url.FullUri.Should().Contain("&offset=100&");
            pages[2].Url.FullUri.Should().Contain("&offset=200&");
        }

        [Test]
        public void should_not_get_unlimited_pages()
        {
            var results = Subject.GetSearchRequests(_gameSearchCriteria);

            results.GetAllTiers().Should().HaveCount(2);

            var pages = results.GetAllTiers().First().Take(500).ToList();

            pages.Count.Should().BeLessThan(500);
        }

        [Test]
        public void should_not_search_by_igdbid_if_not_supported()
        {
            _capabilities.SupportedGameSearchParameters = new[] { "q" };

            var results = Subject.GetSearchRequests(_gameSearchCriteria);

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().NotContain("igdbid=");
            page.Url.Query.Should().Contain("q=Star");
        }

        [Test]
        public void should_search_by_igdbid_if_supported()
        {
            _capabilities.SupportedGameSearchParameters = new[] { "q", "igdbid" };

            var results = Subject.GetSearchRequests(_gameSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("igdbid=11");
        }

        [Test]
        public void should_use_igdbid_search_when_supported()
        {
            _capabilities.SupportedGameSearchParameters = new[] { "q", "igdbid" };
            _capabilities.SupportsAggregateIdSearch = true;

            var results = Subject.GetSearchRequests(_gameSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetTier(0).First().First();

            page.Url.Query.Should().Contain("igdbid=11");
        }

        [Test]
        public void should_not_use_aggregrated_id_search_if_no_ids_supported()
        {
            _capabilities.SupportedGameSearchParameters = new[] { "q" };
            _capabilities.SupportsAggregateIdSearch = true; // Turns true if indexer supplies supportedParams.

            var results = Subject.GetSearchRequests(_gameSearchCriteria);
            results.Tiers.Should().Be(1);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetTier(0).First().First();

            page.Url.Query.Should().Contain("q=");
        }

        [Test]
        public void should_not_use_aggregrated_id_search_if_no_ids_are_known()
        {
            _capabilities.SupportedGameSearchParameters = new[] { "q" };
            _capabilities.SupportsAggregateIdSearch = true; // Turns true if indexer supplies supportedParams.

            _gameSearchCriteria.Game.IgdbId = 0;

            var results = Subject.GetSearchRequests(_gameSearchCriteria);

            var page = results.GetTier(0).First().First();

            page.Url.Query.Should().Contain("q=");
        }

        [Test]
        public void should_fallback_to_q()
        {
            _capabilities.SupportedGameSearchParameters = new[] { "q", "igdbid" };
            _capabilities.SupportsAggregateIdSearch = true;

            var results = Subject.GetSearchRequests(_gameSearchCriteria);
            results.Tiers.Should().Be(2);

            var pageTier2 = results.GetTier(1).First().First();

            pageTier2.Url.Query.Should().NotContain("igdbid=11");
            pageTier2.Url.Query.Should().Contain("q=");
        }

        [Test]
        public void should_encode_raw_title()
        {
            _capabilities.SupportedGameSearchParameters = new[] { "q" };
            _capabilities.TextSearchEngine = "raw";

            var gameRawSearchCriteria = new GameSearchCriteria
            {
                Game = new Games.Game { Title = "Some Game & Title: Words", Year = 2021, IgdbId = 123 },
                SceneTitles = new List<string> { "Some Game & Title: Words" }
            };

            var results = Subject.GetSearchRequests(gameRawSearchCriteria);

            var page = results.GetTier(0).First().First();

            page.Url.Query.Should().Contain("q=Some%20Game%20%26%20Title%3A%20Words");
            page.Url.Query.Should().NotContain(" & ");
            page.Url.Query.Should().Contain("%26");
        }

        [Test]
        public void should_use_clean_title_and_encode()
        {
            _capabilities.SupportedGameSearchParameters = new[] { "q" };
            _capabilities.TextSearchEngine = "sphinx";

            var gameRawSearchCriteria = new GameSearchCriteria
            {
                Game = new Games.Game { Title = "Some Game & Title: Words", Year = 2021, IgdbId = 123 },
                SceneTitles = new List<string> { "Some Game & Title: Words" }
            };

            var results = Subject.GetSearchRequests(gameRawSearchCriteria);

            var page = results.GetTier(0).First().First();

            page.Url.Query.Should().Contain("q=Some%20Game%20and%20Title%20Words%202021");
            page.Url.Query.Should().Contain("and");
            page.Url.Query.Should().NotContain(" & ");
            page.Url.Query.Should().NotContain("%26");
        }
    }
}
