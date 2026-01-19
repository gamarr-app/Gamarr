using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class NewznabRequestGenerator : IIndexerRequestGenerator
    {
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;
        public int MaxPages { get; set; }
        public int PageSize { get; set; }
        public NewznabSettings Settings { get; set; }

        public NewznabRequestGenerator(INewznabCapabilitiesProvider capabilitiesProvider)
        {
            _capabilitiesProvider = capabilitiesProvider;

            MaxPages = 30;
            PageSize = 100;
        }

        private bool SupportsSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportedSearchParameters != null &&
                       capabilities.SupportedSearchParameters.Contains("q");
            }
        }

        private bool SupportsImdbSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportedGameSearchParameters != null &&
                       capabilities.SupportedGameSearchParameters.Contains("imdbid");
            }
        }

        private bool SupportsIgdbSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportedGameSearchParameters != null &&
                       capabilities.SupportedGameSearchParameters.Contains("igdbid");
            }
        }

        private bool SupportsAggregatedIdSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportsAggregateIdSearch;
            }
        }

        private string TextSearchEngine
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.TextSearchEngine;
            }
        }

        private string GameTextSearchEngine
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.GameTextSearchEngine;
            }
        }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

            // Some indexers might forget to enable game search, but normal search still works fine. Thus we force a normal search.
            if (capabilities.SupportedGameSearchParameters != null)
            {
                pageableRequests.Add(GetPagedRequests(MaxPages, Settings.Categories, "game", ""));
            }
            else if (capabilities.SupportedSearchParameters != null)
            {
                pageableRequests.Add(GetPagedRequests(MaxPages, Settings.Categories, "search", ""));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(GameSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            AddGameIdPageableRequests(pageableRequests, MaxPages, Settings.Categories, searchCriteria);

            return pageableRequests;
        }

        private void AddGameIdPageableRequests(IndexerPageableRequestChain chain, int maxPages, IEnumerable<int> categories, SearchCriteriaBase searchCriteria)
        {
            var includeIgdbSearch = SupportsIgdbSearch && searchCriteria.Game.GameMetadata.Value.IgdbId > 0;
            var includeImdbSearch = SupportsImdbSearch && searchCriteria.Game.GameMetadata.Value.ImdbId.IsNotNullOrWhiteSpace();

            if (SupportsAggregatedIdSearch && (includeIgdbSearch || includeImdbSearch))
            {
                var ids = "";

                if (includeIgdbSearch)
                {
                    ids += $"&igdbid={searchCriteria.Game.GameMetadata.Value.IgdbId}";
                }

                if (includeImdbSearch)
                {
                    ids += $"&imdbid={searchCriteria.Game.GameMetadata.Value.ImdbId.Substring(2)}";
                }

                chain.Add(GetPagedRequests(maxPages, categories, "game", ids));
            }
            else
            {
                if (includeIgdbSearch)
                {
                    chain.Add(GetPagedRequests(maxPages,
                        categories,
                        "game",
                        $"&igdbid={searchCriteria.Game.GameMetadata.Value.IgdbId}"));
                }
                else if (includeImdbSearch)
                {
                    chain.Add(GetPagedRequests(maxPages,
                        categories,
                        "game",
                        $"&imdbid={searchCriteria.Game.GameMetadata.Value.ImdbId.Substring(2)}"));
                }
            }

            if (SupportsSearch && searchCriteria.Game.Year > 0)
            {
                chain.AddTier();
                var queryTitles = TextSearchEngine == "raw" ? searchCriteria.SceneTitles : searchCriteria.CleanSceneTitles;

                foreach (var queryTitle in queryTitles)
                {
                    var searchQuery = queryTitle;

                    if (!Settings.RemoveYear)
                    {
                        searchQuery += $" {searchCriteria.Game.Year}";
                    }

                    chain.Add(GetPagedRequests(MaxPages,
                        Settings.Categories,
                        "search",
                        $"&q={NewsnabifyTitle(searchQuery)}"));
                }
            }
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(int maxPages, IEnumerable<int> categories, string searchType, string parameters)
        {
            if (categories.Empty())
            {
                yield break;
            }

            var categoriesQuery = string.Join(",", categories.Distinct());

            var baseUrl = $"{Settings.BaseUrl.TrimEnd('/')}{Settings.ApiPath.TrimEnd('/')}?t={searchType}&cat={categoriesQuery}&extended=1{Settings.AdditionalParameters}";

            if (Settings.ApiKey.IsNotNullOrWhiteSpace())
            {
                baseUrl += "&apikey=" + Settings.ApiKey;
            }

            if (PageSize == 0)
            {
                yield return new IndexerRequest($"{baseUrl}{parameters}", HttpAccept.Rss);
            }
            else
            {
                for (var page = 0; page < maxPages; page++)
                {
                    yield return new IndexerRequest($"{baseUrl}&offset={page * PageSize}&limit={PageSize}{parameters}", HttpAccept.Rss);
                }
            }
        }

        private static string NewsnabifyTitle(string title)
        {
            var newtitle = title.Replace("+", " ");
            return Uri.EscapeDataString(newtitle);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
