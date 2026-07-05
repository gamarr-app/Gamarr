using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.GogLibrary
{
    public class GogLibraryRequestGenerator : IImportListRequestGenerator
    {
        // 50 games per page, 20 pages = 1000 items (MaxNumResultsPerQuery).
        // Paging stops early on the first non-full page.
        private const int MaxPages = 20;

        public GogLibrarySettings Settings { get; set; }
        public Logger Logger { get; set; }

        public virtual ImportListPageableRequestChain GetGames()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            var username = Uri.EscapeDataString(Settings.Username.Trim());

            var requests = new List<ImportListRequest>();

            for (var page = 1; page <= MaxPages; page++)
            {
                var url = $"https://www.gog.com/u/{username}/games/stats?page={page}";
                var request = new ImportListRequest(url, HttpAccept.Json);

                // Let non-2xx responses through so the parser can raise a clear
                // "profile is private / does not exist" error.
                request.HttpRequest.SuppressHttpError = true;

                requests.Add(request);
            }

            pageableRequests.Add(requests);

            return pageableRequests;
        }
    }
}
