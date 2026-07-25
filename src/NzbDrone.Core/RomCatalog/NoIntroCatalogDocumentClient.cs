using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogDocumentClient
    {
        string Fetch(string sourceUrl);
        string FetchDatOMaticNumbered(int systemId);
        string FetchAdvanscene(string sourceUrl);
    }

    public class NoIntroCatalogDocumentClient : INoIntroCatalogDocumentClient
    {
        private const string DatOMaticSourceUrlPrefix = "datomatic://system/";
        private static readonly Regex DownloadTokenRegex = new Regex("<input type=\"submit\" name=\"(?<token>[0-9a-f]{32})\" value=\"Download!!\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly IHttpClient _httpClient;

        public NoIntroCatalogDocumentClient(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string Fetch(string sourceUrl)
        {
            if (sourceUrl.StartsWith(DatOMaticSourceUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var systemId = int.Parse(sourceUrl.Substring(DatOMaticSourceUrlPrefix.Length));
                return FetchDatOMaticNumbered(systemId);
            }

            return _httpClient.Get(new HttpRequest(sourceUrl)).Content;
        }

        public string FetchDatOMaticNumbered(int systemId)
        {
            var prepareUrl = $"https://datomatic.no-intro.org/index.php?page=download&op=dat&s={systemId}";
            _httpClient.Get(new HttpRequest(prepareUrl));

            var prepareRequest = new HttpRequestBuilder(prepareUrl)
                .Post()
                .AddFormParameter("system_selection", systemId)
                .AddFormParameter("sys_list_order", 1)
                .AddFormParameter("format", 0)
                .AddFormParameter("naming", 0)
                .AddFormParameter("numbered", 1)
                .AddFormParameter("inc_bios", 1)
                .AddFormParameter("release_1", 1)
                .AddFormParameter("release_2", 1)
                .AddFormParameter("license_1", 1)
                .AddFormParameter("license_2", 1)
                .AddFormParameter("license_0", 1)
                .AddFormParameter("lifespan_1", 1)
                .AddFormParameter("inc_xroms", 1)
                .AddFormParameter("inc_zroms", 1)
                .AddFormParameter("storage_1", 1)
                .AddFormParameter("storage_2", 1)
                .AddFormParameter("inc_nodump", 0)
                .AddFormParameter("inc_mia", 1)
                .AddFormParameter("dat_dl_2026-05-30", "Prepare")
                .Build();

            var prepareResponse = _httpClient.Post(prepareRequest);
            var token = DownloadTokenRegex.Matches(prepareResponse.Content)
                .Cast<Match>()
                .Select(x => x.Groups["token"].Value)
                .LastOrDefault();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("DAT-o-MATIC did not return a numbered DAT download token");
            }

            var downloadRequest = new HttpRequestBuilder(prepareResponse.Request.Url.FullUri)
                .Post()
                .AddFormParameter(token, "Download!!")
                .Build();
            var downloadResponse = _httpClient.Post(downloadRequest);

            return ExtractDat(downloadResponse.ResponseData);
        }

        public string FetchAdvanscene(string sourceUrl)
        {
            return ExtractDat(_httpClient.Get(new HttpRequest(sourceUrl)).ResponseData);
        }

        private static string ExtractDat(byte[] data)
        {
            if (data.Length >= 2 && data[0] == 'P' && data[1] == 'K')
            {
                using var stream = new MemoryStream(data);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                var entry = archive.Entries.FirstOrDefault(x => x.Name.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)) ?? archive.Entries.First();
                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream);
                return reader.ReadToEnd();
            }

            return System.Text.Encoding.UTF8.GetString(data);
        }
    }
}
