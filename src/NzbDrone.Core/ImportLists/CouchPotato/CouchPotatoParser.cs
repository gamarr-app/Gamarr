using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.CouchPotato
{
    public class CouchPotatoParser : IParseImportListResponse
    {
        private ImportListResponse _importListResponse;

        public IList<ImportListGame> ParseResponse(ImportListResponse importListResponse)
        {
            _importListResponse = importListResponse;

            var games = new List<ImportListGame>();

            if (!PreProcess(_importListResponse))
            {
                return games;
            }

            var jsonResponse = JsonConvert.DeserializeObject<CouchPotatoResponse>(_importListResponse.Content);

            // no games were return
            if (jsonResponse.total == 0)
            {
                return games;
            }

            var responseData = jsonResponse.games;

            foreach (var item in responseData)
            {
                var igdbid = item.info?.igdb_id ?? 0;

                // Fix weird error reported by Madmanali93
                if (item.type != null && item.releases != null)
                {
                    // if there are no releases at all the game wasn't found on CP, so return games
                    if (!item.releases.Any() && item.type == "game")
                    {
                        games.AddIfNotNull(new ImportListGame()
                        {
                            Title = item.title,
                            ImdbId = item.info.imdb,
                            IgdbId = igdbid
                        });
                    }
                    else
                    {
                        // snatched,missing,available,downloaded
                        // done,seeding
                        var isCompleted = item.releases.Any(rel => (rel.status == "done" || rel.status == "seeding"));
                        if (!isCompleted)
                        {
                            games.AddIfNotNull(new ImportListGame()
                            {
                                Title = item.title,
                                ImdbId = item.info.imdb,
                                IgdbId = igdbid
                            });
                        }
                    }
                }
            }

            return games;
        }

        protected virtual bool PreProcess(ImportListResponse importListResponse)
        {
            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse, "List API call resulted in an unexpected StatusCode [{0}]", importListResponse.HttpResponse.StatusCode);
            }

            if (importListResponse.HttpResponse.Headers.ContentType != null && importListResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                importListResponse.HttpRequest.Headers.Accept != null && !importListResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(importListResponse, "List responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}
