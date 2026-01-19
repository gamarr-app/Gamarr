using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotatoRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentPotatoSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests("list", null, null));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string mode, int? tvdbId, string query, params object[] args)
        {
            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl)
                .Accept(HttpAccept.Json);

            requestBuilder.AddQueryParam("passkey", Settings.Passkey);
            if (!string.IsNullOrWhiteSpace(Settings.User))
            {
                requestBuilder.AddQueryParam("user", Settings.User);
            }
            else
            {
                requestBuilder.AddQueryParam("user", "");
            }

            requestBuilder.AddQueryParam("search", "-");

            yield return new IndexerRequest(requestBuilder.Build());
        }

        private IEnumerable<IndexerRequest> GetGameRequest(GameSearchCriteria searchCriteria)
        {
            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl)
                 .Accept(HttpAccept.Json);

            requestBuilder.AddQueryParam("passkey", Settings.Passkey);

            if (!string.IsNullOrWhiteSpace(Settings.User))
            {
                requestBuilder.AddQueryParam("user", Settings.User);
            }
            else
            {
                requestBuilder.AddQueryParam("user", "");
            }

            if (searchCriteria.Game.ImdbId.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("imdbid", searchCriteria.Game.ImdbId);
            }
            else
            {
                requestBuilder.AddQueryParam("search", $"{searchCriteria.Game.Title} {searchCriteria.Game.Year}");
            }

            yield return new IndexerRequest(requestBuilder.Build());
        }

        public IndexerPageableRequestChain GetSearchRequests(GameSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetGameRequest(searchCriteria));
            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
