using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.SteamLibrary
{
    public class SteamLibraryParser : IParseImportListResponse
    {
        private readonly bool _playedOnly;

        public SteamLibraryParser(bool playedOnly)
        {
            _playedOnly = playedOnly;
        }

        public IList<ImportListGame> ParseResponse(ImportListResponse importListResponse)
        {
            if (importListResponse.HttpResponse.StatusCode == HttpStatusCode.Unauthorized ||
                importListResponse.HttpResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new ImportListException(importListResponse,
                    "Steam API rejected the request. Check that the Steam API Key is valid and the profile's game details are public.");
            }

            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse,
                    "Steam API call returned unexpected StatusCode [{0}]",
                    importListResponse.HttpResponse.StatusCode);
            }

            var jsonResponse = JsonConvert.DeserializeObject<SteamLibraryResponse>(importListResponse.Content);

            if (jsonResponse?.Response?.Games == null || jsonResponse.Response.Games.Count == 0)
            {
                return new List<ImportListGame>();
            }

            return jsonResponse.Response.Games
                .Where(g => g.AppId > 0)
                .Where(g => !_playedOnly || g.PlaytimeForever > 0)
                .Select(g => new ImportListGame
                {
                    SteamAppId = g.AppId,
                    Title = string.IsNullOrWhiteSpace(g.Name) ? $"Steam App {g.AppId}" : g.Name
                })
                .ToList();
        }
    }

    public class SteamLibraryResponse
    {
        [JsonProperty("response")]
        public SteamLibraryResponseBody Response { get; set; }
    }

    public class SteamLibraryResponseBody
    {
        [JsonProperty("game_count")]
        public int GameCount { get; set; }

        [JsonProperty("games")]
        public List<SteamLibraryGame> Games { get; set; }
    }

    public class SteamLibraryGame
    {
        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("playtime_forever")]
        public int PlaytimeForever { get; set; }
    }
}
