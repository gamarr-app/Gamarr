using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.Steam.Resource;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.MetadataSource.Steam
{
    /// <summary>
    /// Steam Store API metadata provider.
    /// NO API KEY REQUIRED - works out of the box.
    /// Uses the unofficial but stable Steam Store API endpoints.
    /// </summary>
    public class SteamStoreProxy : ISearchForNewGame
    {
        private const string StoreApiBaseUrl = "https://store.steampowered.com/api/";

        private readonly IHttpClient _httpClient;
        private readonly IGameService _gameService;
        private readonly Logger _logger;

        public SteamStoreProxy(
            IHttpClient httpClient,
            IGameService gameService,
            Logger logger)
        {
            _httpClient = httpClient;
            _gameService = gameService;
            _logger = logger;
        }

        public Tuple<GameMetadata, List<Games.Credits.Credit>> GetGameInfo(int steamAppId)
        {
            _logger.Debug("Fetching Steam game info for App ID {0}", steamAppId);

            var request = new HttpRequestBuilder($"{StoreApiBaseUrl}appdetails")
                .AddQueryParam("appids", steamAppId)
                .AddQueryParam("l", "english")
                .Accept(HttpAccept.Json)
                .Build();

            try
            {
                var response = _httpClient.Get(request);
                var json = JObject.Parse(response.Content);

                var appData = json[steamAppId.ToString()];
                if (appData == null || !appData["success"].Value<bool>())
                {
                    _logger.Warn("Steam returned no data for App ID {0}", steamAppId);
                    return new Tuple<GameMetadata, List<Games.Credits.Credit>>(null, new List<Games.Credits.Credit>());
                }

                var data = appData["data"].ToObject<SteamGameData>();
                if (data == null || data.Type != "game")
                {
                    _logger.Debug("Steam App ID {0} is not a game (type: {1})", steamAppId, data?.Type);
                    return new Tuple<GameMetadata, List<Games.Credits.Credit>>(null, new List<Games.Credits.Credit>());
                }

                var game = MapSteamGame(data);
                return new Tuple<GameMetadata, List<Games.Credits.Credit>>(game, new List<Games.Credits.Credit>());
            }
            catch (HttpException ex)
            {
                _logger.Error(ex, "Failed to fetch Steam game info for App ID {0}", steamAppId);
                return new Tuple<GameMetadata, List<Games.Credits.Credit>>(null, new List<Games.Credits.Credit>());
            }
        }

        public List<Game> SearchForNewGame(string title)
        {
            _logger.Debug("Searching Steam for '{0}'", title);

            var results = SearchGames(title);

            return results.Select(metadata =>
            {
                var existingGame = _gameService.FindBySteamAppId(metadata.SteamAppId);
                return existingGame ?? new Game { GameMetadata = metadata };
            }).ToList();
        }

        public List<GameMetadata> SearchGames(string query, int limit = 15)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<GameMetadata>();
            }

            var request = new HttpRequestBuilder($"{StoreApiBaseUrl}storesearch/")
                .AddQueryParam("term", query)
                .AddQueryParam("l", "english")
                .AddQueryParam("cc", "US")
                .Accept(HttpAccept.Json)
                .Build();

            try
            {
                var response = _httpClient.Get<SteamSearchResponse>(request);

                if (response.Resource?.Items == null || !response.Resource.Items.Any())
                {
                    _logger.Debug("No results from Steam store search for '{0}'", query);
                    return new List<GameMetadata>();
                }

                return response.Resource.Items
                    .Where(item => item.Type == "app" || item.Type == "game")
                    .Take(limit)
                    .Select(MapSearchResult)
                    .Where(g => g != null)
                    .ToList();
            }
            catch (HttpException ex)
            {
                _logger.Error(ex, "Failed to search Steam for '{0}'", query);
                return new List<GameMetadata>();
            }
        }

        public GameMetadata GetGameBySteamAppId(int steamAppId)
        {
            var result = GetGameInfo(steamAppId);
            return result?.Item1;
        }

        public GameMetadata MapGameToIgdbGame(GameMetadata game)
        {
            // Steam doesn't have IGDB IDs, just return the game as-is
            return game;
        }

        private GameMetadata MapSteamGame(SteamGameData data)
        {
            var game = new GameMetadata
            {
                SteamAppId = data.Steam_Appid,
                Title = data.Name,
                CleanTitle = data.Name.CleanGameTitle(),
                SortTitle = GameTitleNormalizer.Normalize(data.Name, data.Steam_Appid),
                Overview = StripHtml(data.About_The_Game ?? data.Detailed_Description ?? data.Short_Description),
                Website = data.Website,
                Status = GetStatus(data.Release_Date),
                Year = ParseYear(data.Release_Date?.Date),
                Genres = data.Genres?.Select(g => g.Description).ToList() ?? new List<string>(),
                Platforms = MapPlatforms(data.Platforms),
                Developer = data.Developers?.FirstOrDefault(),
                Publisher = data.Publishers?.FirstOrDefault(),
                Certification = MapAgeRating(data.Required_Age),
                LastInfoSync = DateTime.UtcNow
            };

            // Parse release date
            if (data.Release_Date != null && !string.IsNullOrEmpty(data.Release_Date.Date))
            {
                var releaseDate = ParseReleaseDate(data.Release_Date.Date);
                if (releaseDate.HasValue)
                {
                    game.PhysicalRelease = releaseDate.Value;
                    game.DigitalRelease = releaseDate.Value;
                }
            }

            // Set ratings - only Metacritic since Steam doesn't have IGDB ratings
            game.Ratings = new Ratings();

            if (data.Metacritic != null && data.Metacritic.Score > 0)
            {
                game.Ratings.Metacritic = new RatingChild
                {
                    Value = data.Metacritic.Score,
                    Type = RatingType.Critic
                };
            }

            // Set images
            game.Images = new List<MediaCover.MediaCover>();

            // Use Steam's library poster image (600x900) - proper vertical poster format
            var posterUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{data.Steam_Appid}/library_600x900.jpg";
            game.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Poster, posterUrl));

            // Use header image as secondary fanart if no background
            if (!string.IsNullOrEmpty(data.Background_Raw ?? data.Background))
            {
                game.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Fanart, data.Background_Raw ?? data.Background));
            }
            else if (!string.IsNullOrEmpty(data.Header_Image))
            {
                game.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Fanart, data.Header_Image));
            }

            if (data.Screenshots != null)
            {
                foreach (var screenshot in data.Screenshots.Take(5))
                {
                    game.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Screenshot, screenshot.Path_Full));
                }
            }

            return game;
        }

        private GameMetadata MapSearchResult(SteamSearchItem item)
        {
            if (item == null)
            {
                return null;
            }

            var game = new GameMetadata
            {
                SteamAppId = item.Id,
                Title = item.Name,
                CleanTitle = item.Name.CleanGameTitle(),
                SortTitle = GameTitleNormalizer.Normalize(item.Name, item.Id),
                Status = GameStatusType.Released,
                Images = new List<MediaCover.MediaCover>(),
                Ratings = new Ratings(),
                Platforms = MapPlatforms(item.Platforms)
            };

            // Use Steam's library poster image (600x900) - much better quality than tiny search result
            var posterUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{item.Id}/library_600x900.jpg";
            game.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Poster, posterUrl));

            // Also add header image as fanart
            var headerUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{item.Id}/header.jpg";
            game.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Fanart, headerUrl));

            // Add Metacritic rating if available
            if (!string.IsNullOrEmpty(item.Metascore) && int.TryParse(item.Metascore, out var metascore) && metascore > 0)
            {
                game.Ratings.Metacritic = new RatingChild
                {
                    Value = metascore,
                    Type = RatingType.Critic
                };
            }

            return game;
        }

        private List<GamePlatform> MapPlatforms(SteamPlatforms platforms)
        {
            var result = new List<GamePlatform>();

            if (platforms == null)
            {
                return result;
            }

            if (platforms.Windows)
            {
                result.Add(new GamePlatform
                {
                    Name = "PC (Windows)",
                    Abbreviation = "win",
                    Family = PlatformFamily.PC
                });
            }

            if (platforms.Mac)
            {
                result.Add(new GamePlatform
                {
                    Name = "Mac",
                    Abbreviation = "mac",
                    Family = PlatformFamily.Mac
                });
            }

            if (platforms.Linux)
            {
                result.Add(new GamePlatform
                {
                    Name = "Linux",
                    Abbreviation = "linux",
                    Family = PlatformFamily.Linux
                });
            }

            return result;
        }

        private GameStatusType GetStatus(SteamReleaseDate releaseDate)
        {
            if (releaseDate == null)
            {
                return GameStatusType.Released;
            }

            if (releaseDate.Coming_Soon)
            {
                return GameStatusType.Announced;
            }

            return GameStatusType.Released;
        }

        private int ParseYear(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return 0;
            }

            var date = ParseReleaseDate(dateString);
            return date?.Year ?? 0;
        }

        private DateTime? ParseReleaseDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return null;
            }

            string[] formats =
            {
                "MMM d, yyyy",
                "MMMM d, yyyy",
                "MMM dd, yyyy",
                "MMMM dd, yyyy",
                "d MMM, yyyy",
                "dd MMM, yyyy",
                "yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(
                    dateString,
                    format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var result))
                {
                    return result;
                }
            }

            if (DateTime.TryParse(
                dateString,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var generalResult))
            {
                return generalResult;
            }

            var yearMatch = Regex.Match(dateString, @"\b(19|20)\d{2}\b");
            if (yearMatch.Success && int.TryParse(yearMatch.Value, out var year))
            {
                return new DateTime(year, 1, 1);
            }

            return null;
        }

        private string MapAgeRating(int requiredAge)
        {
            return requiredAge switch
            {
                0 => null,
                3 => "ESRB-E",
                7 => "ESRB-E",
                10 => "ESRB-E10+",
                13 => "ESRB-T",
                16 => "ESRB-T",
                17 => "ESRB-M",
                18 => "ESRB-AO",
                _ => requiredAge >= 18 ? "ESRB-M" : null
            };
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            var result = Regex.Replace(html, "<.*?>", string.Empty);
            result = System.Net.WebUtility.HtmlDecode(result);
            result = Regex.Replace(result, @"\s+", " ").Trim();

            return result;
        }
    }
}
