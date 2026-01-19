using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.TMDb.Person
{
    public class TMDbPersonRequestGenerator : IImportListRequestGenerator
    {
        public TMDbPersonSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public Logger Logger { get; set; }

        public virtual ImportListPageableRequestChain GetGames()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetGamesRequest());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetGamesRequest()
        {
            Logger.Info("Importing TMDb games from person: {0}", Settings.PersonId);

            var requestBuilder = RequestBuilder.Create()
                .SetSegment("api", "3")
                .SetSegment("route", "person")
                .SetSegment("id", Settings.PersonId)
                .SetSegment("secondaryRoute", "/game_credits");

            yield return new ImportListRequest(requestBuilder.Accept(HttpAccept.Json).Build());
        }
    }
}
