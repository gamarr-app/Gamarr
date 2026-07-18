using NzbDrone.Common.Http;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogDocumentClient
    {
        string Fetch(string sourceUrl);
    }

    public class NoIntroCatalogDocumentClient : INoIntroCatalogDocumentClient
    {
        private readonly IHttpClient _httpClient;

        public NoIntroCatalogDocumentClient(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string Fetch(string sourceUrl)
        {
            return _httpClient.Get(new HttpRequest(sourceUrl)).Content;
        }
    }
}
