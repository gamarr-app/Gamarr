using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.GogWishlist
{
    public class GogWishlistRequestGenerator : IImportListRequestGenerator
    {
        // 100 products per page, 10 pages = 1000 items (MaxNumResultsPerQuery).
        // Paging stops early on the first non-full page.
        private const int MaxPages = 10;

        public GogWishlistSettings Settings { get; set; }
        public Logger Logger { get; set; }

        public virtual ImportListPageableRequestChain GetGames()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            var username = Uri.EscapeDataString(Settings.Username.Trim());

            var requests = new List<ImportListRequest>();

            for (var page = 1; page <= MaxPages; page++)
            {
                var url = $"https://www.gog.com/u/{username}/wishlist?page={page}";
                var request = new ImportListRequest(url, HttpAccept.Html);

                // Let non-2xx responses through so the parser can raise a clear
                // "profile does not exist / wishlist is not public" error.
                request.HttpRequest.SuppressHttpError = true;

                requests.Add(request);
            }

            pageableRequests.Add(requests);

            return pageableRequests;
        }
    }
}
