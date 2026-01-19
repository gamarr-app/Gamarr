using System.Collections.Generic;
using System.Net.Http;
using NzbDrone.Core.Notifications.Trakt;

namespace NzbDrone.Core.ImportLists.Trakt.List
{
    public class TraktListRequestGenerator : IImportListRequestGenerator
    {
        private readonly ITraktProxy _traktProxy;
        public TraktListSettings Settings { get; set; }

        public TraktListRequestGenerator(ITraktProxy traktProxy)
        {
            _traktProxy = traktProxy;
        }

        public virtual ImportListPageableRequestChain GetGames()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetGamesRequest());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetGamesRequest()
        {
            var link = string.Empty;

            // Trakt slug rules:
            // - replace all special characters with a dash
            // - replaces multiple dashes with a single dash
            // - allows underscore as a valid character
            // - does not trim underscore from the end
            // - allows multiple underscores in a row
            var listName = Parser.Parser.ToUrlSlug(Settings.Listname.Trim(), true, "-", "-");
            link += $"users/{Settings.Username.Trim()}/lists/{listName}/items/games?limit={Settings.Limit}";

            var request = new ImportListRequest(_traktProxy.BuildRequest(link, HttpMethod.Get, Settings.AccessToken));

            yield return request;
        }
    }
}
