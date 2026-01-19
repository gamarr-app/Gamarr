using System.Collections.Generic;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.TMDb.Keyword
{
    public class TMDbKeywordParser : TMDbParser
    {
        public override IList<ImportListGame> ParseResponse(ImportListResponse importResponse)
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

            foreach (var game in jsonResponse.Results)
            {
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
