using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.MediaCover;

namespace NzbDrone.Core.ImportLists.TMDb
{
    public class TMDbParser : IParseImportListResponse
    {
        public virtual IList<ImportListGame> ParseResponse(ImportListResponse importResponse)
        {
            var games = new List<ImportListGame>();

            if (!PreProcess(importResponse))
            {
                return games;
            }

            var jsonResponse = JsonConvert.DeserializeObject<GameSearchResource>(importResponse.Content);

            // no games were return
            if (jsonResponse == null)
            {
                return games;
            }

            return jsonResponse.Results.SelectList(MapListGame);
        }

        protected ImportListGame MapListGame(GameResultResource gameResult)
        {
            var game =  new ImportListGame
            {
                IgdbId = gameResult.Id,
                Title = gameResult.Title,
            };

            if (gameResult.ReleaseDate.IsNotNullOrWhiteSpace() && DateTime.TryParse(gameResult.ReleaseDate, out var releaseDate))
            {
                game.Year = releaseDate.Year;
            }

            return game;
        }

        private MediaCover.MediaCover MapPosterImage(string path)
        {
            if (path.IsNotNullOrWhiteSpace())
            {
                return new MediaCover.MediaCover(MediaCoverTypes.Poster, $"https://image.igdb.org/t/p/original{path}");
            }

            return null;
        }

        protected virtual bool PreProcess(ImportListResponse listResponse)
        {
            if (listResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(listResponse,
                    "TMDb API call resulted in an unexpected StatusCode [{0}]",
                    listResponse.HttpResponse.StatusCode);
            }

            if (listResponse.HttpResponse.Headers.ContentType != null &&
                listResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                listResponse.HttpRequest.Headers.Accept != null &&
                !listResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new ImportListException(listResponse,
                    "TMDb responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}
