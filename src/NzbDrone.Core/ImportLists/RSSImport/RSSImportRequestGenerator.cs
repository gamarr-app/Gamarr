using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.RSSImport
{
    public class RSSImportRequestGenerator : IImportListRequestGenerator
    {
        public RSSImportSettings Settings { get; set; }

        public virtual ImportListPageableRequestChain GetGames()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetGames(null));

            return pageableRequests;
        }

        // public ImportListPageableRequestChain GetSearchRequests(GameSearchCriteria searchCriteria)
        // {
        //    return new ImportListPageableRequestChain();
        // }
        private IEnumerable<ImportListRequest> GetGames(string searchParameters)
        {
            var request = new ImportListRequest($"{Settings.Link.Trim()}", HttpAccept.Rss);
            yield return request;
        }
    }
}
