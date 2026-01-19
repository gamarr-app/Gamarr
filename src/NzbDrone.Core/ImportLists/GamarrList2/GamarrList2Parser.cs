using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.GamarrList2
{
    public class GamarrList2Parser : IParseImportListResponse
    {
        public virtual IList<ImportListGame> ParseResponse(ImportListResponse importListResponse)
        {
            var importResponse = importListResponse;

            var games = new List<ImportListGame>();

            if (!PreProcess(importResponse))
            {
                return games;
            }

            var jsonResponse = JsonConvert.DeserializeObject<List<GamarrList2Resource>>(importResponse.Content);

            // no games were return
            if (jsonResponse == null)
            {
                return games;
            }

            return jsonResponse.SelectList(m => new ImportListGame { IgdbId = m.IgdbId });
        }

        protected virtual bool PreProcess(ImportListResponse listResponse)
        {
            if (listResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(listResponse,
                    "Gamarr API call resulted in an unexpected StatusCode [{0}]",
                    listResponse.HttpResponse.StatusCode);
            }

            if (listResponse.HttpResponse.Headers.ContentType != null &&
                listResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                listResponse.HttpRequest.Headers.Accept != null &&
                !listResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(listResponse,
                    "Gamarr API responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}
