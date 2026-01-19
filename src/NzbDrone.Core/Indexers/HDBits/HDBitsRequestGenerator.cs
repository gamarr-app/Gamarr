using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.HDBits
{
    public class HDBitsRequestGenerator : IIndexerRequestGenerator
    {
        public HDBitsSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(new TorrentQuery()));
            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(GameSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var query = new TorrentQuery();

            // IMDb search removed - IMDb is a movie database and doesn't apply to games
            // TODO: HDBits is a movie-focused tracker and may not be suitable for game searches
            // For now, use a basic query without IMDb ID filtering
            // The indexer may return no results for games

            // Attempt to add any search parameters that are available
            TryAddSearchParameters(query, searchCriteria);
            pageableRequests.Add(GetRequest(query));

            return pageableRequests;
        }

        private bool TryAddSearchParameters(TorrentQuery query, SearchCriteriaBase searchCriteria)
        {
            // IMDb search removed - IMDb is a movie database and doesn't apply to games
            // HDBits relies heavily on IMDb for identification, which won't work for games
            // TODO: Consider whether HDBits indexer should be disabled/removed for game manager
            // as it's a movie-focused tracker

            // Return true to indicate we can proceed with a basic search
            // even without IMDb filtering
            return true;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IEnumerable<IndexerRequest> GetRequest(TorrentQuery query)
        {
            var request = new HttpRequestBuilder(Settings.BaseUrl)
                .Resource("/api/torrents")
                .Build();

            request.Method = HttpMethod.Post;
            const string appJson = "application/json";
            request.Headers.Accept = appJson;
            request.Headers.ContentType = appJson;

            query.Username = Settings.Username;
            query.Passkey = Settings.ApiKey;

            query.Category = Settings.Categories.ToArray();
            query.Codec = Settings.Codecs.ToArray();
            query.Medium = Settings.Mediums.ToArray();

            query.Limit = 100;

            request.SetContent(query.ToJson());
            request.ContentSummary = query.ToJson(Formatting.None);

            yield return new IndexerRequest(request);
        }
    }
}
