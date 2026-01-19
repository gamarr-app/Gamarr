#pragma warning disable CS0618
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Notifications.Trakt.Resource;

namespace NzbDrone.Core.ImportLists.Trakt.Popular
{
    public class TraktPopularParser : TraktParser
    {
        private readonly TraktPopularSettings _settings;
        private ImportListResponse _importResponse;

        public TraktPopularParser(TraktPopularSettings settings)
        {
            _settings = settings;
        }

        public override IList<ImportListGame> ParseResponse(ImportListResponse importResponse)
        {
            _importResponse = importResponse;

            var games = new List<ImportListGame>();

            if (!PreProcess(_importResponse))
            {
                return games;
            }

            var jsonResponse = new List<TraktGameResource>();

            if (_settings.TraktListType == (int)TraktPopularListType.Popular)
            {
                jsonResponse = STJson.Deserialize<List<TraktGameResource>>(_importResponse.Content);
            }
            else
            {
                jsonResponse = STJson.Deserialize<List<TraktListResource>>(_importResponse.Content).SelectList(c => c.Game);
            }

            // no games were return
            if (jsonResponse == null)
            {
                return games;
            }

            foreach (var game in jsonResponse)
            {
                games.AddIfNotNull(new ImportListGame()
                {
                    Title = game.Title,
                    ImdbId = game.Ids.Imdb,
                    IgdbId = game.Ids.Igdb,
                    Year = game.Year ?? 0
                });
            }

            return games;
        }
    }
}
