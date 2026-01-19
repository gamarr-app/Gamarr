using System.Collections.Generic;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.Simkl
{
    public class SimklParser : IParseImportListResponse
    {
        private ImportListResponse _importResponse;

        public SimklParser()
        {
        }

        public virtual IList<ImportListGame> ParseResponse(ImportListResponse importResponse)
        {
            _importResponse = importResponse;

            var game = new List<ImportListGame>();

            if (!PreProcess(_importResponse))
            {
                return game;
            }

            var jsonResponse = STJson.Deserialize<SimklResponse>(_importResponse.Content);

            // no shows were return
            if (jsonResponse == null)
            {
                return game;
            }

            foreach (var show in jsonResponse.Games)
            {
                game.AddIfNotNull(new ImportListGame()
                {
                    Title = show.Game.Title,
                    IgdbId = int.TryParse(show.Game.Ids.Igdb, out var igdbId) ? igdbId : 0,
                    ImdbId = show.Game.Ids.Imdb
                });
            }

            return game;
        }

        protected virtual bool PreProcess(ImportListResponse netImportResponse)
        {
            if (netImportResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(netImportResponse, "Simkl API call resulted in an unexpected StatusCode [{0}]", netImportResponse.HttpResponse.StatusCode);
            }

            if (netImportResponse.HttpResponse.Headers.ContentType != null && netImportResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                netImportResponse.HttpRequest.Headers.Accept != null && !netImportResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(netImportResponse, "Simkl API responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}
