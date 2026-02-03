using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.SteamWishlist
{
    public class SteamWishlistParser : IParseImportListResponse
    {
        public IList<ImportListGame> ParseResponse(ImportListResponse importListResponse)
        {
            var games = new List<ImportListGame>();

            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse,
                    "Steam API call resulted in an unexpected StatusCode [{0}]",
                    importListResponse.HttpResponse.StatusCode);
            }

            var jsonResponse = JsonConvert.DeserializeObject<SteamWishlistResponse>(importListResponse.Content);

            if (jsonResponse?.Response?.Items == null || jsonResponse.Response.Items.Count == 0)
            {
                return games;
            }

            return jsonResponse.Response.Items
                .Where(item => item.AppId > 0)
                .Select(item => new ImportListGame
                {
                    SteamAppId = item.AppId,
                    Title = $"Steam App {item.AppId}"
                })
                .ToList();
        }
    }

    public class SteamWishlistResponse
    {
        [JsonProperty("response")]
        public SteamWishlistResponseBody Response { get; set; }
    }

    public class SteamWishlistResponseBody
    {
        [JsonProperty("items")]
        public List<SteamWishlistItem> Items { get; set; }
    }

    public class SteamWishlistItem
    {
        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("date_added")]
        public long DateAdded { get; set; }
    }
}
