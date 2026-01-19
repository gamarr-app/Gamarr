using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.GamarrList
{
    public class GamarrListParser : IParseImportListResponse
    {
        public IList<ImportListGame> ParseResponse(ImportListResponse importListResponse)
        {
            var importResponse = importListResponse;

            var games = new List<ImportListGame>();

            if (!PreProcess(importResponse))
            {
                return games;
            }

            var jsonResponse = JsonConvert.DeserializeObject<List<GameResultResource>>(importResponse.Content);

            // no games were return
            if (jsonResponse == null)
            {
                return games;
            }

            return jsonResponse
                .Where(m => m.Id > 0)
                .SelectList(m => new ImportListGame { IgdbId = m.Id });
        }

        protected virtual bool PreProcess(ImportListResponse importListResponse)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<GamarrErrors>(importListResponse.HttpResponse.Content);

                if (error != null && error.Errors != null && error.Errors.Count != 0)
                {
                    throw new GamarrListException(error);
                }
            }
            catch (JsonSerializationException)
            {
                // No error!
            }

            if (importListResponse.HttpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new HttpException(importListResponse.HttpRequest, importListResponse.HttpResponse);
            }

            return true;
        }
    }
}
