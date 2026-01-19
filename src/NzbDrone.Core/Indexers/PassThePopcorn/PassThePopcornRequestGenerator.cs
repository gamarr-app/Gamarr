using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.PassThePopcorn
{
    /// <summary>
    /// PassThePopcorn is a movie-focused tracker.
    /// TODO: Consider whether this indexer should be disabled/removed for game manager
    /// as it's a movie-focused tracker that uses IMDb IDs which don't apply to games.
    /// </summary>
    public class PassThePopcornRequestGenerator : IIndexerRequestGenerator
    {
        private readonly PassThePopcornSettings _settings;

        public PassThePopcornRequestGenerator(PassThePopcornSettings settings)
        {
            _settings = settings;
        }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(null));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(GameSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            // IMDb search removed - PassThePopcorn is a movie tracker that uses IMDb IDs
            // which don't apply to games. Fall back to title-based search.
            // TODO: Consider whether PassThePopcorn should be disabled for game searches

            if (searchCriteria.Game.Year > 0)
            {
                foreach (var queryTitle in searchCriteria.CleanSceneTitles)
                {
                    pageableRequests.Add(GetRequest($"{queryTitle}&year={searchCriteria.Game.Year}"));
                }
            }
            else
            {
                foreach (var queryTitle in searchCriteria.CleanSceneTitles)
                {
                    pageableRequests.Add(GetRequest(queryTitle));
                }
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var request =
                new IndexerRequest(
                    $"{_settings.BaseUrl.Trim().TrimEnd('/')}/torrents.php?action=advanced&json=noredirect&grouping=0&order_by=time&order_way=desc&searchstr={searchParameters}",
                    HttpAccept.Json);

            request.HttpRequest.Headers.Add("ApiUser", _settings.APIUser);
            request.HttpRequest.Headers.Add("ApiKey", _settings.APIKey);

            yield return request;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
