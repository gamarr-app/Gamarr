using System.Collections.Generic;
using System.Net.Http;
using NzbDrone.Core.Notifications.Trakt;

namespace NzbDrone.Core.ImportLists.Trakt.Popular
{
    public class TraktPopularRequestGenerator : IImportListRequestGenerator
    {
        private readonly ITraktProxy _traktProxy;
        public TraktPopularSettings Settings { get; set; }

        public TraktPopularRequestGenerator(ITraktProxy traktProxy)
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

            var filtersAndLimit = $"?years={Settings.Years}&genres={Settings.Genres.ToLower()}&ratings={Settings.Rating}&certifications={Settings.Certification.ToLower()}&limit={Settings.Limit}{Settings.TraktAdditionalParameters}";

            switch (Settings.TraktListType)
            {
                case (int)TraktPopularListType.Trending:
                    link += "games/trending";
                    break;
                case (int)TraktPopularListType.Popular:
                    link += "games/popular";
                    break;
                case (int)TraktPopularListType.Anticipated:
                    link += "games/anticipated";
                    break;
                case (int)TraktPopularListType.BoxOffice:
                    link += "games/boxoffice";
                    break;
                case (int)TraktPopularListType.TopWatchedByWeek:
                    link += "games/watched/weekly";
                    break;
                case (int)TraktPopularListType.TopWatchedByMonth:
                    link += "games/watched/monthly";
                    break;
#pragma warning disable CS0612
                case (int)TraktPopularListType.TopWatchedByYear:
#pragma warning restore CS0612
                    link += "games/watched/yearly";
                    break;
                case (int)TraktPopularListType.TopWatchedByAllTime:
                    link += "games/watched/all";
                    break;
                case (int)TraktPopularListType.RecommendedByWeek:
                    link += "games/recommended/weekly";
                    break;
                case (int)TraktPopularListType.RecommendedByMonth:
                    link += "games/recommended/monthly";
                    break;
#pragma warning disable CS0612
                case (int)TraktPopularListType.RecommendedByYear:
#pragma warning restore CS0612
                    link += "games/recommended/yearly";
                    break;
                case (int)TraktPopularListType.RecommendedByAllTime:
                    link += "games/recommended/all";
                    break;
            }

            link += filtersAndLimit;

            var request = new ImportListRequest(_traktProxy.BuildRequest(link, HttpMethod.Get, Settings.AccessToken));

            yield return request;
        }
    }
}
