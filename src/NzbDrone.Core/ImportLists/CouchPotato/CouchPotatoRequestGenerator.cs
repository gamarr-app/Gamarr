using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.CouchPotato
{
    public class CouchPotatoRequestGenerator : IImportListRequestGenerator
    {
        public CouchPotatoSettings Settings { get; set; }

        public virtual ImportListPageableRequestChain GetGames()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetGames(null));

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetGames(string searchParameters)
        {
            var urlBase = "";
            if (!string.IsNullOrWhiteSpace(Settings.UrlBase))
            {
                urlBase = Settings.UrlBase.StartsWith("/") ? Settings.UrlBase : $"/{Settings.UrlBase}";
            }

            var status = "";

            if (Settings.OnlyActive)
            {
                status = "?status=active";
            }

            var request = new ImportListRequest($"{Settings.Link.Trim()}:{Settings.Port}{urlBase}/api/{Settings.ApiKey}/game.list/{status}", HttpAccept.Json);
            yield return request;
        }
    }
}
