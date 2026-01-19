using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListRequestGenerator : IIndexerRequestGenerator
    {
        public FileListSettings Settings { get; set; }
        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest("latest-torrents", ""));
            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(GameSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            // IMDb search removed - IMDb is a movie database and doesn't apply to games
            // TODO: FileList may need to add IGDB support for game searches
            // For now, fall back to title-based search

            if (searchCriteria.Game.Year > 0)
            {
                foreach (var queryTitle in searchCriteria.CleanSceneTitles)
                {
                    var titleYearSearchQuery = $"{queryTitle}+{searchCriteria.Game.Year}";
                    pageableRequests.Add(GetRequest("search-torrents", $"&type=name&query={titleYearSearchQuery.Trim()}"));
                }
            }
            else
            {
                foreach (var queryTitle in searchCriteria.CleanSceneTitles)
                {
                    pageableRequests.Add(GetRequest("search-torrents", $"&type=name&query={queryTitle.Trim()}"));
                }
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchType, string parameters)
        {
            if (Settings.Categories.Empty())
            {
                yield break;
            }

            var categoriesQuery = string.Join(",", Settings.Categories.Distinct());

            var baseUrl = $"{Settings.BaseUrl.TrimEnd('/')}/api.php?action={searchType}&category={categoriesQuery}{parameters}";

            var request = new IndexerRequest(baseUrl, HttpAccept.Json);
            request.HttpRequest.Credentials = new BasicNetworkCredential(Settings.Username.Trim(), Settings.Passkey.Trim());

            yield return request;
        }
    }
}
