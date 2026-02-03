using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.SteamWishlist
{
    public class SteamWishlistRequestGenerator : IImportListRequestGenerator
    {
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

            var doc = XDocument.Load(new StringReader(response.Content));
            var steamId64 = doc.Root?.Element("steamID64")?.Value;

            if (string.IsNullOrWhiteSpace(steamId64))
            {
                throw new System.Exception($"Could not resolve Steam vanity URL '{input}' to a Steam64 ID. Make sure the profile exists and is public.");
            }

            Logger.Debug("Resolved Steam vanity URL '{0}' to Steam64 ID '{1}'", input, steamId64);

            return steamId64;
        }
    }
}
