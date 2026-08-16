using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.SteamWishlist
{
    public class SteamWishlistRequestGenerator : IImportListRequestGenerator
    {
        private static readonly Regex SteamId64Regex = new (@"<steamID64>\s*(?<id>\d+)\s*</steamID64>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SteamWishlistSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public Logger Logger { get; set; }

        public virtual ImportListPageableRequestChain GetGames()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            var steamId = ResolveSteamId(Settings.SteamUserId.Trim());

            var url = $"https://api.steampowered.com/IWishlistService/GetWishlist/v1/?steamid={steamId}";
            var request = new ImportListRequest(url, HttpAccept.Json);
            request.HttpRequest.SuppressHttpError = true;

            pageableRequests.Add(new List<ImportListRequest> { request });
            return pageableRequests;
        }

        private string ResolveSteamId(string input)
        {
            if (input.All(char.IsDigit) && input.Length > 5)
            {
                return input;
            }

            Logger.Debug("Resolving Steam vanity URL '{0}' to Steam64 ID", input);

            var request = new HttpRequest($"https://steamcommunity.com/id/{input}/?xml=1");
            request.AllowAutoRedirect = true;
            var response = HttpClient.Get(request);

            var steamId64 = ParseSteamId64(response.Content);

            if (string.IsNullOrWhiteSpace(steamId64))
            {
                throw new System.Exception($"Could not resolve Steam vanity URL '{input}' to a Steam64 ID. Make sure the profile exists and is public.");
            }

            Logger.Debug("Resolved Steam vanity URL '{0}' to Steam64 ID '{1}'", input, steamId64);

            return steamId64;
        }

        // Steam serves the profile XML with unescaped entities in user-supplied fields (summary, group
        // names, ...), which makes it invalid XML. We only ever want steamID64, so fall back to a plain
        // text match rather than failing the whole import list on someone else's malformed profile blurb.
        private string ParseSteamId64(string content)
        {
            try
            {
                return XDocument.Load(new StringReader(content)).Root?.Element("steamID64")?.Value;
            }
            catch (XmlException ex)
            {
                Logger.Debug(ex, "Steam profile XML is malformed, falling back to extracting steamID64 directly");

                var match = SteamId64Regex.Match(content);

                return match.Success ? match.Groups["id"].Value : null;
            }
        }
    }
}
