using System.Collections.Generic;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.TMDb.Company
{
    public class TMDbCompanyRequestGenerator : IImportListRequestGenerator
    {
        public TMDbCompanySettings Settings { get; set; }
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
            Logger.Info("Importing TMDb games from company: {0}", Settings.CompanyId);

            var requestBuilder = RequestBuilder.Create()
                .SetSegment("api", "3")
                .SetSegment("route", "discover")
                .SetSegment("id", "game")
                .SetSegment("secondaryRoute", "");

            requestBuilder.AddQueryParam("with_companies", Settings.CompanyId);

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
