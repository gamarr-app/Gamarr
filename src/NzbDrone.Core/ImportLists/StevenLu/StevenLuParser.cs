using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.StevenLu
{
    public class StevenLuParser : IParseImportListResponse
    {
        private ImportListResponse _importResponse;

        public IList<ImportListGame> ParseResponse(ImportListResponse importResponse)
        {
            _importResponse = importResponse;

            var games = new List<ImportListGame>();

            if (!PreProcess(_importResponse))
            {
                return games;
            }

            var jsonResponse = JsonConvert.DeserializeObject<List<StevenLuResponse>>(_importResponse.Content);

            // no games were return
            if (jsonResponse == null)
            {
                return games;
            }

            foreach (var item in jsonResponse)
            {
                games.AddIfNotNull(new ImportListGame()
                {
                    Title = item.title,
                    ImdbId = item.imdb_id,
                });
            }

            return games;
        }

        protected virtual bool PreProcess(ImportListResponse importListResponse)
        {
            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse, "StevenLu API call resulted in an unexpected StatusCode [{0}]", importListResponse.HttpResponse.StatusCode);
            }

            if (importListResponse.HttpResponse.Headers.ContentType != null && importListResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                importListResponse.HttpRequest.Headers.Accept != null && !importListResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(importListResponse, "StevenLu responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}
