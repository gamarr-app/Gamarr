using System.Collections.Generic;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.TMDb.List
{
    public class TMDbListParser : TMDbParser
    {
        public override IList<ImportListGame> ParseResponse(ImportListResponse importResponse)
        {
            var games = new List<ImportListGame>();

            if (!PreProcess(importResponse))
            {
                return games;
            }

            var jsonResponse = JsonConvert.DeserializeObject<ListResponseResource>(importResponse.Content);

            // no games were return
            if (jsonResponse == null)
            {
                return games;
            }

            foreach (var game in jsonResponse.Results)
            {
                // Media Type is not Game
                if (game.MediaType != "game")
                {
                    continue;
                }

                // Games with no Year Fix
                if (string.IsNullOrWhiteSpace(game.ReleaseDate))
                {
                    continue;
                }

                games.AddIfNotNull(MapListGame(game));
            }

            return games;
        }
    }
}
