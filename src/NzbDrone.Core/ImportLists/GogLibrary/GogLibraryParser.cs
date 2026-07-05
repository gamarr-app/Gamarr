using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.Gog;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.GogLibrary
{
    /// <summary>
    /// Parses the public GOG profile games list
    /// (https://www.gog.com/u/{username}/games/stats?page=N), a paginated JSON
    /// document of owned games. Private profiles return HTTP 403, nonexistent
    /// profiles return HTTP 404.
    /// </summary>
    public class GogLibraryParser : IParseImportListResponse
    {
        public IList<ImportListGame> ParseResponse(ImportListResponse importListResponse)
        {
            return ParseProducts(importListResponse)
                .Select(p => new ImportListGame
                {
                    Title = p.Title,
                    Year = p.Year
                })
                .ToList();
        }

        public IList<GogProduct> ParseProducts(ImportListResponse importListResponse)
        {
            var statusCode = importListResponse.HttpResponse.StatusCode;

            if (statusCode == HttpStatusCode.Forbidden)
            {
                throw new ImportListException(importListResponse,
                    "GOG profile games list is private. Set your GOG profile and games list to public in GOG Privacy Settings.");
            }

            if (statusCode == HttpStatusCode.NotFound)
            {
                throw new ImportListException(importListResponse,
                    "GOG profile not found. Make sure the username is correct (as in gog.com/u/<username>).");
            }

            if (statusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse,
                    "GOG games list request resulted in an unexpected StatusCode [{0}]",
                    statusCode);
            }

            var data = JsonConvert.DeserializeObject<GogLibraryResponse>(importListResponse.Content);

            if (data?.Embedded?.Items == null || data.Embedded.Items.Count == 0)
            {
                return new List<GogProduct>();
            }

            // Guard against page-number clamping duplicating the last page.
            var requestedPage = GetRequestedPage(importListResponse);

            if (requestedPage > 0 && data.Page > 0 && data.Page != requestedPage)
            {
                return new List<GogProduct>();
            }

            return data.Embedded.Items
                .Where(i => i.Game != null && long.TryParse(i.Game.Id, out var id) && id > 0)
                .Select(i => new GogProduct
                {
                    GogId = long.Parse(i.Game.Id),
                    Title = i.Game.Title
                })
                .ToList();
        }

        private static int GetRequestedPage(ImportListResponse importListResponse)
        {
            var match = Regex.Match(importListResponse.HttpRequest.Url.FullUri, @"[?&]page=(\d+)");

            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
    }

    public class GogLibraryResponse
    {
        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pages")]
        public int Pages { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("_embedded")]
        public GogLibraryEmbedded Embedded { get; set; }
    }

    public class GogLibraryEmbedded
    {
        [JsonProperty("items")]
        public List<GogLibraryItem> Items { get; set; }
    }

    public class GogLibraryItem
    {
        [JsonProperty("game")]
        public GogLibraryGame Game { get; set; }
    }

    public class GogLibraryGame
    {
        // GOG returns the product id as a string in this endpoint
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
