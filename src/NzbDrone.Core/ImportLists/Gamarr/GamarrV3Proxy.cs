using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Gamarr
{
    public interface IGamarrV3Proxy
    {
        List<GamarrGame> GetGames(GamarrSettings settings);
        List<GamarrProfile> GetProfiles(GamarrSettings settings);
        List<GamarrRootFolder> GetRootFolders(GamarrSettings settings);
        List<GamarrTag> GetTags(GamarrSettings settings);
        ValidationFailure Test(GamarrSettings settings);
    }

    public class GamarrV3Proxy : IGamarrV3Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public GamarrV3Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<GamarrGame> GetGames(GamarrSettings settings)
        {
            var requestBuilder = BuildRequest("/api/v3/game", settings);

            requestBuilder.AddQueryParam("excludeLocalCovers", true);

            return Execute<GamarrGame>(requestBuilder, settings);
        }

        public List<GamarrProfile> GetProfiles(GamarrSettings settings)
        {
            return Execute<GamarrProfile>(BuildRequest("/api/v3/qualityprofile", settings), settings);
        }

        public List<GamarrRootFolder> GetRootFolders(GamarrSettings settings)
        {
            return Execute<GamarrRootFolder>(BuildRequest("api/v3/rootfolder", settings), settings);
        }

        public List<GamarrTag> GetTags(GamarrSettings settings)
        {
            return Execute<GamarrTag>(BuildRequest("/api/v3/tag", settings), settings);
        }

        public ValidationFailure Test(GamarrSettings settings)
        {
            try
            {
                GetGames(settings);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Error(ex, "API Key is invalid");
                    return new ValidationFailure("ApiKey", "API Key is invalid");
                }

                if (ex.Response.HasHttpRedirect)
                {
                    _logger.Error(ex, "Gamarr returned redirect and is invalid");
                    return new ValidationFailure("BaseUrl", "Gamarr URL is invalid, are you missing a URL base?");
                }

                _logger.Error(ex, "Unable to connect to import list.");
                return new ValidationFailure(string.Empty, $"Unable to connect to import list: {ex.Message}. Check the log surrounding this error for details.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to connect to import list.");
                return new ValidationFailure(string.Empty, $"Unable to connect to import list: {ex.Message}. Check the log surrounding this error for details.");
            }

            return null;
        }

        private HttpRequestBuilder BuildRequest(string resource, GamarrSettings settings)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');

            return new HttpRequestBuilder(baseUrl).Resource(resource)
                .Accept(HttpAccept.Json)
                .SetHeader("X-Api-Key", settings.ApiKey);
        }

        private List<TResource> Execute<TResource>(HttpRequestBuilder requestBuilder, GamarrSettings settings)
        {
            if (settings.BaseUrl.IsNullOrWhiteSpace() || settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new List<TResource>();
            }

            var request = requestBuilder.Build();

            var response = _httpClient.Get(request);

            if ((int)response.StatusCode >= 300)
            {
                throw new HttpException(response);
            }

            var results = JsonConvert.DeserializeObject<List<TResource>>(response.Content);

            return results;
        }
    }
}
