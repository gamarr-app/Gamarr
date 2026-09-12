using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Emby
{
    public class MediaBrowserProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public MediaBrowserProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public void TestConnection(MediaBrowserSettings settings)
        {
            var path = "/System/Configuration";
            var request = BuildRequest(path, settings).Build();

            var response = _httpClient.Get(request);
            _logger.Trace("Response: {0}", response.Content);
        }

        public void Notify(MediaBrowserSettings settings, string title, string message)
        {
            var path = "/Notifications/Admin";
            var request = BuildRequest(path, settings).Build();
            request.Headers.ContentType = "application/json";
            request.LogHttpError = false;

            request.SetContent(new
            {
                Name = title,
                Description = message,
                ImageUrl = "https://raw.github.com/gamarr-app/Gamarr/develop/Logo/64.png"
            }.ToJson());

            ProcessRequest(request);
        }

        public HashSet<string> GetPaths(MediaBrowserSettings settings, Game game)
        {
            var path = "/Items";

            // NameStartsWith uses the sort title, which is not the game title
            var request = BuildRequest(path, settings)
                .AddQueryParam("recursive", "true")
                .AddQueryParam("includeItemTypes", "Game")
                .AddQueryParam("fields", "Path,ProviderIds")
                .AddQueryParam("years", game.Year)
                .Build();

            try
            {
                var paths = ProcessGetRequest<MediaBrowserItems>(request).Items.GroupBy(item =>
                {
                    if (item is { ProviderIds.Igdb: int igdbid } && igdbid != 0 && igdbid == game.IgdbId)
                    {
                        return MediaBrowserMatchQuality.Id;
                    }

                    if (item is { Name: var name } && name == game.Title)
                    {
                        return MediaBrowserMatchQuality.Name;
                    }

                    return MediaBrowserMatchQuality.None;
                }, item => item.Path).OrderBy(group => (int)group.Key).First();

                if (paths.Key == MediaBrowserMatchQuality.None)
                {
                    _logger.Trace("Could not find game by name");

                    return new HashSet<string>();
                }

                _logger.Trace("Found game by name/id: {0}", string.Join(" ", paths));

                return paths.ToHashSet();
            }
            catch (InvalidOperationException)
            {
                _logger.Trace("Could not find game by name.");

                return new HashSet<string>();
            }
        }

        public void Update(MediaBrowserSettings settings, string gamePath, string updateType)
        {
            var path = "/Library/Media/Updated";
            var request = BuildRequest(path, settings).Build();
            request.Headers.ContentType = "application/json";

            request.SetContent(new
            {
                Updates = new[]
                {
                    new
                    {
                        Path = gamePath,
                        UpdateType = updateType
                    }
                }
            }.ToJson());

            ProcessRequest(request);
        }

        private T ProcessGetRequest<T>(HttpRequest request)
            where T : new()
        {
            var response = _httpClient.Get<T>(request);
            _logger.Trace("Response: {0}", response.Content);

            CheckForError(response);

            return response.Resource;
        }

        private string ProcessRequest(HttpRequest request)
        {
            var response = _httpClient.Post(request);
            _logger.Trace("Response: {0}", response.Content);

            CheckForError(response);

            return response.Content;
        }

        private string GetUrl(MediaBrowserSettings settings)
        {
            var scheme = settings.UseSsl ? "https" : "http";
            return $@"{scheme}://{settings.Address}";
        }

        private HttpRequestBuilder BuildRequest(string path, MediaBrowserSettings settings)
        {
            var url = GetUrl(settings);
            var request = new HttpRequestBuilder(url).Resource(path);

            request.Headers.Add("X-MediaBrowser-Token", settings.ApiKey);
            request.Headers.Add("Authorization", $"MediaBrowser Token=\"{settings.ApiKey}\"");

            return request;
        }

        private void CheckForError(HttpResponse response)
        {
            _logger.Debug("Looking for error in response: {0}", response);

            if (response.HasHttpServerError)
            {
                throw new HttpException(response.Request, response);
            }
        }
    }
}
