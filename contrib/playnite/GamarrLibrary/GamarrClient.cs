using System;
using System.Collections.Generic;
using System.Net.Http;
using GamarrLibrary.Mapping;
using Newtonsoft.Json;

namespace GamarrLibrary
{
    /// <summary>
    /// Thin HTTP client for the Gamarr v3 API.
    /// </summary>
    public class GamarrClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public GamarrClient(string baseUrl, string apiKey)
        {
            _baseUrl = GamarrMapper.NormalizeBaseUrl(baseUrl)
                ?? throw new ArgumentException("Gamarr base URL is not configured.", nameof(baseUrl));

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey ?? string.Empty);
        }

        public List<GamarrGameDto> GetGames()
        {
            var json = GetString($"{_baseUrl}/api/v3/game");
            return JsonConvert.DeserializeObject<List<GamarrGameDto>>(json) ?? new List<GamarrGameDto>();
        }

        /// <summary>
        /// Throws on any connectivity/auth problem; used by settings verification.
        /// </summary>
        public void TestConnection()
        {
            GetString($"{_baseUrl}/api/v3/system/status");
        }

        private string GetString(string url)
        {
            // Playnite plugins run on .NET Framework; sync-over-async on a
            // background import thread is acceptable and keeps the LibraryPlugin
            // GetGames override simple.
            using (var response = _httpClient.GetAsync(url).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
