using System.Collections.Generic;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Notifications.Trakt.Resource;

namespace NzbDrone.Core.ImportLists.Trakt
{
    public class TraktParser : IParseImportListResponse
    {
        private ImportListResponse _importResponse;

        public virtual IList<ImportListGame> ParseResponse(ImportListResponse importResponse)
        {
            _importResponse = importResponse;

            var games = new List<ImportListGame>();

            if (!PreProcess(_importResponse))
            {
                return games;
            }

            var jsonResponse = STJson.Deserialize<List<TraktListResource>>(_importResponse.Content);

            // no games were return
            if (jsonResponse == null)
            {
                return games;
            }

            foreach (var game in jsonResponse)
            {
                games.AddIfNotNull(new ImportListGame()
                {
                    Title = game.Game.Title,
                    ImdbId = game.Game.Ids.Imdb,
                    IgdbId = game.Game.Ids.Igdb,
                    Year = game.Game.Year ?? 0
                });
            }

            return games;
        }

        protected virtual bool PreProcess(ImportListResponse importListResponse)
        {
            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse, "Trakt API call resulted in an unexpected StatusCode [{0}]", importListResponse.HttpResponse.StatusCode);
            }

            if (importListResponse.HttpResponse.Headers.ContentType != null && importListResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                importListResponse.HttpRequest.Headers.Accept != null && !importListResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(importListResponse, "Trakt API responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}
