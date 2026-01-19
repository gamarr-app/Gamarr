using System.Collections.Generic;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.TMDb.Keyword
{
    public class TMDbKeywordRequestGenerator : IImportListRequestGenerator
    {
        public TMDbKeywordSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public Logger Logger { get; set; }
        public int MaxPages { get; set; }

        public virtual ImportListPageableRequestChain GetGames()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetGamesRequest());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetGamesRequest()
        {
            Logger.Info("Importing TMDb games from keyword Id: {0}", Settings.KeywordId);

            var requestBuilder = RequestBuilder.Create()
                .SetSegment("api", "3")
                .SetSegment("route", "discover")
                .SetSegment("id", "game")
                .SetSegment("secondaryRoute", "");

            requestBuilder.AddQueryParam("with_keywords", Settings.KeywordId);

            var jsonResponse = JsonConvert.DeserializeObject<GameSearchResource>(HttpClient.Execute(requestBuilder.Build()).Content);

            MaxPages = jsonResponse.TotalPages;

            for (var pageNumber = 1; pageNumber <= MaxPages; pageNumber++)
            {
                requestBuilder.AddQueryParam("page", pageNumber, true);

                var request = requestBuilder.Build();

                Logger.Debug("Importing TMDb games from: {0}", request.Url);

                yield return new ImportListRequest(request);
            }
        }
    }
}
