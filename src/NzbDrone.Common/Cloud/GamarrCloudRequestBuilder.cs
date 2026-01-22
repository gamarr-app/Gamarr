using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IGamarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory IGDB { get; }
        IHttpRequestBuilderFactory GamarrMetadata { get; }
    }

    public class GamarrCloudRequestBuilder : IGamarrCloudRequestBuilder
    {
        public GamarrCloudRequestBuilder()
        {
            // Use GitHub API for update checks
            Services = new HttpRequestBuilder("https://api.github.com/repos/gamarr-app/Gamarr/")
                .SetHeader("Accept", "application/vnd.github.v3+json")
                .SetHeader("User-Agent", "Gamarr")
                .CreateFactory();

            IGDB = new HttpRequestBuilder("https://api.thegamedb.org/{api}/{route}/{id}{secondaryRoute}")
                .SetHeader("Authorization", $"Bearer {AuthToken}")
                .CreateFactory();

            GamarrMetadata = new HttpRequestBuilder("https://api.github.com/repos/gamarr-app/Gamarr/")
                .SetHeader("Accept", "application/vnd.github.v3+json")
                .SetHeader("User-Agent", "Gamarr")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; private set; }
        public IHttpRequestBuilderFactory IGDB { get; private set; }
        public IHttpRequestBuilderFactory GamarrMetadata { get; private set; }

        public string AuthToken => "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIxYTczNzMzMDE5NjFkMDNmOTdmODUzYTg3NmRkMTIxMiIsInN1YiI6IjU4NjRmNTkyYzNhMzY4MGFiNjAxNzUzNCIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.gh1BwogCCKOda6xj9FRMgAAj_RYKMMPC3oNlcBtlmwk";
    }
}
