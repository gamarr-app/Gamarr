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

        private bool SupportsSteamSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportedGameSearchParameters != null &&
                       capabilities.SupportedGameSearchParameters.Contains("steamappid");
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
            // Primary identifier - Steam App ID
            var includeSteamSearch = SupportsSteamSearch && searchCriteria.Game.GameMetadata.Value.SteamAppId > 0;

            // Secondary identifier - IGDB ID
            var includeIgdbSearch = SupportsIgdbSearch && searchCriteria.Game.GameMetadata.Value.IgdbId > 0;

            // Search by Steam App ID first (primary identifier)
            if (includeSteamSearch)
            {
                chain.Add(GetPagedRequests(maxPages,
                    categories,
                    "game",
                    $"&steamappid={searchCriteria.Game.GameMetadata.Value.SteamAppId}"));
            }

            // Fall back to IGDB ID if no Steam search or as additional search
            if (includeIgdbSearch)
            {
                if (includeSteamSearch)
                {
                    chain.AddTier();
                }

                chain.Add(GetPagedRequests(maxPages,
                    categories,
                    "game",
                    $"&igdbid={searchCriteria.Game.GameMetadata.Value.IgdbId}"));
            }

            if (SupportsSearch)
            {
                chain.AddTier();
                var queryTitles = TextSearchEngine == "raw" ? searchCriteria.SceneTitles : searchCriteria.CleanSceneTitles;

                foreach (var queryTitle in queryTitles)
                {
                    var searchTitle = queryTitle;

                    // Include year in search for disambiguation when available (unless RemoveYear is set)
                    if (searchCriteria.Game.Year > 0 && !Settings.RemoveYear)
                    {
                        searchTitle = $"{queryTitle} {searchCriteria.Game.Year}";
                    }

                    chain.Add(GetPagedRequests(MaxPages,
                        Settings.Categories,
                        "search",
                        $"&q={NewsnabifyTitle(searchTitle)}"));
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
