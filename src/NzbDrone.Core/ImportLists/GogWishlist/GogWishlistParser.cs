using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.ImportLists.Gog;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.GogWishlist
{
    /// <summary>
    /// Parses the public GOG profile wishlist page (https://www.gog.com/u/{username}/wishlist).
    /// The page server-renders the wishlist as "var gogData = {...};" containing a
    /// products array with id, title and releaseDate. Private or nonexistent
    /// profiles return HTTP 404.
    /// </summary>
    public class GogWishlistParser : IParseImportListResponse
    {
        private const string GogDataMarker = "var gogData = ";

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

            if (statusCode == HttpStatusCode.NotFound)
            {
                throw new ImportListException(importListResponse,
                    "GOG profile not found. Make sure the username is correct and both the profile and its wishlist are set to public in GOG Privacy Settings.");
            }

            if (statusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse,
                    "GOG wishlist request resulted in an unexpected StatusCode [{0}]",
                    statusCode);
            }

            var json = ExtractGogData(importListResponse.Content);

            if (json == null)
            {
                throw new ImportListException(importListResponse,
                    "Could not find wishlist data on the GOG profile page. The wishlist may not be public.");
            }

            var data = JsonConvert.DeserializeObject<GogWishlistData>(json);

            if (data?.Products == null || data.Products.Count == 0)
            {
                return new List<GogProduct>();
            }

            // GOG clamps out-of-range page numbers to the last page instead of
            // returning an empty list; treat a mismatched page as empty so the
            // paging loop terminates and items aren't duplicated.
            var requestedPage = GetRequestedPage(importListResponse);

            if (requestedPage > 0 && data.Page > 0 && data.Page != requestedPage)
            {
                return new List<GogProduct>();
            }

            return data.Products
                .Where(p => p.Id > 0 && p.IsGame)
                .Select(p => new GogProduct
                {
                    GogId = p.Id,
                    Title = p.Title,
                    Year = GetYear(p.ReleaseDate)
                })
                .ToList();
        }

        private static int GetRequestedPage(ImportListResponse importListResponse)
        {
            var match = Regex.Match(importListResponse.HttpRequest.Url.FullUri, @"[?&]page=(\d+)");

            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static int GetYear(JToken releaseDate)
        {
            if (releaseDate == null || releaseDate.Type == JTokenType.Null)
            {
                return 0;
            }

            long value;

            if (releaseDate.Type == JTokenType.Integer)
            {
                value = releaseDate.Value<long>();
            }
            else if (releaseDate.Type == JTokenType.String && long.TryParse(releaseDate.Value<string>(), out var parsed))
            {
                value = parsed;
            }
            else
            {
                return 0;
            }

            // Plain year (e.g. upcoming titles) vs unix timestamp
            if (value is >= 1900 and <= 2100)
            {
                return (int)value;
            }

            if (value > 100000000)
            {
                return DateTimeOffset.FromUnixTimeSeconds(value).Year;
            }

            return 0;
        }

        /// <summary>
        /// Extracts the JSON object assigned to "var gogData = " using a small
        /// brace-matching scanner (string-literal aware, so braces inside product
        /// descriptions can't break extraction).
        /// </summary>
        internal static string ExtractGogData(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            var markerIndex = html.IndexOf(GogDataMarker, StringComparison.Ordinal);

            if (markerIndex < 0)
            {
                return null;
            }

            var start = html.IndexOf('{', markerIndex + GogDataMarker.Length);

            if (start < 0)
            {
                return null;
            }

            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var i = start; i < html.Length; i++)
            {
                var c = html[i];

                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                }
                else if (c == '"')
                {
                    inString = true;
                }
                else if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return html.Substring(start, i - start + 1);
                    }
                }
            }

            return null;
        }
    }

    public class GogWishlistData
    {
        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        [JsonProperty("totalProducts")]
        public int TotalProducts { get; set; }

        [JsonProperty("products")]
        public List<GogWishlistProduct> Products { get; set; }
    }

    public class GogWishlistProduct
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("releaseDate")]
        public JToken ReleaseDate { get; set; }

        [JsonProperty("isGame")]
        public bool IsGame { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
