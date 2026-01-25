using System;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.MetadataSource.IGDB
{
    /// <summary>
    /// Handles Twitch OAuth authentication for IGDB API.
    /// Tokens are cached and automatically refreshed before expiry.
    /// </summary>
    public interface IIgdbAuthService
    {
        string GetAccessToken();
        string ClientId { get; }
    }

    public class IgdbAuthService : IIgdbAuthService
    {
        private const string TokenUrl = "https://id.twitch.tv/oauth2/token";

        private readonly IConfigService _configService;
        private readonly IHttpClient _httpClient;
        private readonly ICached<IgdbToken> _tokenCache;
        private readonly Logger _logger;

        public IgdbAuthService(
            IConfigService configService,
            IHttpClient httpClient,
            ICacheManager cacheManager,
            Logger logger)
        {
            _configService = configService;
            _httpClient = httpClient;
            _tokenCache = cacheManager.GetCache<IgdbToken>(GetType());
            _logger = logger;
        }

        public string ClientId => IsMockMode() ? "mock_client_id" : _configService.IgdbClientId;

        public string GetAccessToken()
        {
            // In mock mode, return a fake token to allow mock data to be used
            if (IsMockMode())
            {
                _logger.Debug("Mock mode enabled, returning fake IGDB access token");
                return "mock_access_token";
            }

            var cachedToken = _tokenCache.Find("igdb_token");

            if (cachedToken != null && !cachedToken.IsExpired)
            {
                return cachedToken.AccessToken;
            }

            var newToken = FetchNewToken();
            if (newToken != null)
            {
                // Cache with 1 hour buffer before expiry
                _tokenCache.Set("igdb_token", newToken, TimeSpan.FromSeconds(newToken.ExpiresIn - 3600));
                return newToken.AccessToken;
            }

            _logger.Error("Failed to obtain IGDB access token");
            return null;
        }

        private static bool IsMockMode()
        {
            // Check both uppercase and lowercase variants due to case-sensitive
            // environment variables on Linux and StringDictionary lowercasing keys
            var envValue = Environment.GetEnvironmentVariable("GAMARR_MOCK_METADATA")
                        ?? Environment.GetEnvironmentVariable("gamarr_mock_metadata");

            return !string.IsNullOrEmpty(envValue) &&
                   (envValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    envValue.Equals("1", StringComparison.OrdinalIgnoreCase));
        }

        private IgdbToken FetchNewToken()
        {
            var clientId = _configService.IgdbClientId;
            var clientSecret = _configService.IgdbClientSecret;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.Warn("IGDB credentials not configured. Please set IgdbClientId and IgdbClientSecret in settings.");

                // TODO: Return a demo/placeholder token for development
                return null;
            }

            try
            {
                var requestBuilder = new HttpRequestBuilder(TokenUrl)
                    .Post()
                    .AddQueryParam("client_id", clientId)
                    .AddQueryParam("client_secret", clientSecret)
                    .AddQueryParam("grant_type", "client_credentials");

                var request = requestBuilder.Build();
                var response = _httpClient.Execute(request);

                if (response.HasHttpError)
                {
                    _logger.Error("Failed to fetch IGDB token: {0}", response.Content);
                    return null;
                }

                var token = JsonConvert.DeserializeObject<IgdbToken>(response.Content);
                token.ObtainedAt = DateTime.UtcNow;

                _logger.Debug("Successfully obtained IGDB access token");
                return token;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching IGDB access token");
                return null;
            }
        }
    }

    public class IgdbToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        public DateTime ObtainedAt { get; set; }

        public bool IsExpired => DateTime.UtcNow > ObtainedAt.AddSeconds(ExpiresIn - 300);
    }
}
