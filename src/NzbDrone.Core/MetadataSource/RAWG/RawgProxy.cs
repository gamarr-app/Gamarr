using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.RAWG.Resource;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.MetadataSource.RAWG
{
    public class RawgProxy : IProvideGameInfo, ISearchForNewGame
    {
        private const string RawgApiBaseUrl = "https://api.rawg.io/api/";

        // Conservative rate limit to be a good API citizen (2 requests per second)
        private const int RateLimitRequestsPerSecond = 2;

        private static readonly Queue<DateTime> RequestTimestamps = new Queue<DateTime>();
        private static readonly object RateLimitLock = new object();

        private readonly IHttpClient _httpClient;
        private readonly IConfigService _configService;
        private readonly IGameService _gameService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly Logger _logger;

        public RawgProxy(
            IHttpClient httpClient,
            IConfigService configService,
            IGameService gameService,
            IGameMetadataService gameMetadataService,
            Logger logger)
        {
            _httpClient = httpClient;
            _configService = configService;
            _gameService = gameService;
            _gameMetadataService = gameMetadataService;
            _logger = logger;
        }

        public GameMetadata GetGameInfoBySteamAppId(int steamAppId)
        {
            // RAWG doesn't support direct Steam lookup; use AggregateGameInfoProxy for Steam games
            return null;
        }

        public GameMetadata GetGameInfo(int rawgId)
        {
            var apiKey = _configService.RawgApiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.Warn("RAWG API key not configured. Please set it in Settings -> General.");
                throw new InvalidOperationException("RAWG API key not configured");
            }

            var request = new HttpRequestBuilder($"{RawgApiBaseUrl}games/{rawgId}")
                .AddQueryParam("key", apiKey)
                .Accept(HttpAccept.Json)
                .Build();

            EnforceRateLimit();
            var response = _httpClient.Get<RawgGameResource>(request);

            if (response.Resource == null)
            {
                throw new GameNotFoundException(rawgId);
            }

            var game = MapRawgGame(response.Resource);

            // Fetch suggested games and store as RawgRecommendations
            var slug = response.Resource.Slug ?? rawgId.ToString();
            game.RawgRecommendations = GetSuggestedGames(slug, 10);

            return game;
        }

        public GameMetadata GetGameBySteamAppId(int steamAppId)
        {
            // RAWG can search by stores parameter with Steam ID
            var apiKey = _configService.RawgApiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.Warn("RAWG API key not configured.");
                return null;
            }

            // Search RAWG by Steam store ID
            var request = new HttpRequestBuilder($"{RawgApiBaseUrl}games")
                .AddQueryParam("key", apiKey)
                .AddQueryParam("stores", "1") // Steam store ID in RAWG
                .AddQueryParam("search", steamAppId.ToString())
                .AddQueryParam("page_size", 5)
                .Accept(HttpAccept.Json)
                .Build();

            try
            {
                EnforceRateLimit();
                var response = _httpClient.Get<RawgSearchResult>(request);
                var game = response.Resource?.Results?.FirstOrDefault();

                if (game == null)
                {
                    _logger.Debug("No RAWG game found for Steam App ID {0}", steamAppId);
                    return null;
                }

                var metadata = MapRawgGame(game);
                metadata.SteamAppId = steamAppId;
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to lookup Steam App ID {0} in RAWG", steamAppId);
                return null;
            }
        }

        public GameCollection GetCollectionInfo(int collectionId)
        {
            // RAWG doesn't have direct collection support like IGDB
            // Return null for now - could potentially use game_series in future
            return null;
        }

        public List<GameMetadata> GetBulkGameInfo(List<int> rawgIds)
        {
            var games = new List<GameMetadata>();

            // Limit to 20 to prevent excessive API calls
            foreach (var id in rawgIds.Take(20))
            {
                try
                {
                    var result = GetGameInfo(id);
                    if (result != null)
                    {
                        games.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get game info for RAWG ID {0}", id);
                }
            }

            return games;
        }

        public List<GameMetadata> GetTrendingGames()
        {
            // Get recently released games sorted by popularity/additions
            // RAWG "dates" param filters by release date range
            var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3).ToString("yyyy-MM-dd");
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return SearchGamesWithDates($"{threeMonthsAgo},{today}", ordering: "-added", pageSize: 20);
        }

        public List<GameMetadata> GetPopularGames()
        {
            // Get highly rated games from the last 2 years
            var twoYearsAgo = DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd");
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return SearchGamesWithDates($"{twoYearsAgo},{today}", ordering: "-rating", pageSize: 20);
        }

        public HashSet<int> GetChangedGames(DateTime startTime)
        {
            // RAWG doesn't have a changes endpoint
            // Return empty set - games will be refreshed through normal means
            return new HashSet<int>();
        }

        public List<int> GetSimilarGames(string title, int igdbId)
        {
            // For RAWG, get suggested games based on title search
            var slug = FindGameSlug(title);
            if (string.IsNullOrEmpty(slug))
            {
                return new List<int>();
            }

            return GetSuggestedGames(slug, 10);
        }

        public List<Game> SearchForNewGame(string title)
        {
            var results = SearchGames(title, pageSize: 15);

            return results.Select(metadata =>
            {
                // Check by RawgId first (since RAWG results have RawgId set)
                var existingGame = _gameService.FindByRawgId(metadata.RawgId);
                return existingGame ?? new Game { GameMetadata = metadata };
            }).ToList();
        }

        public GameMetadata MapGameToIgdbGame(GameMetadata game)
        {
            if (game.IgdbId > 0)
            {
                var existingGame = _gameMetadataService.FindByIgdbId(game.IgdbId);
                if (existingGame != null)
                {
                    return existingGame;
                }

                try
                {
                    return GetGameInfo(game.IgdbId);
                }
                catch
                {
                    return game;
                }
            }

            return game;
        }

        private List<GameMetadata> SearchGames(string query, string ordering = null, int pageSize = 10)
        {
            return SearchGamesWithDates(null, query, ordering, pageSize);
        }

        private List<GameMetadata> SearchGamesWithDates(string dates, string query = null, string ordering = null, int pageSize = 10)
        {
            var apiKey = _configService.RawgApiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.Warn("RAWG API key not configured. Please set it in Settings -> General.");
                return new List<GameMetadata>();
            }

            var requestBuilder = new HttpRequestBuilder($"{RawgApiBaseUrl}games")
                .AddQueryParam("key", apiKey)
                .AddQueryParam("page_size", pageSize)
                .Accept(HttpAccept.Json);

            if (!string.IsNullOrWhiteSpace(query))
            {
                requestBuilder.AddQueryParam("search", query);
                requestBuilder.AddQueryParam("search_precise", "true");
            }

            if (!string.IsNullOrWhiteSpace(dates))
            {
                requestBuilder.AddQueryParam("dates", dates);
            }

            if (!string.IsNullOrWhiteSpace(ordering))
            {
                requestBuilder.AddQueryParam("ordering", ordering);
            }

            var request = requestBuilder.Build();

            try
            {
                EnforceRateLimit();
                var response = _httpClient.Get<RawgSearchResult>(request);

                if (response.Resource?.Results == null)
                {
                    return new List<GameMetadata>();
                }

                return response.Resource.Results
                    .Select(MapRawgGameBasic)
                    .Where(g => g != null)
                    .ToList();
            }
            catch (HttpException ex)
            {
                _logger.Error(ex, "Failed to search RAWG for '{0}'", query);
                return new List<GameMetadata>();
            }
        }

        private GameMetadata MapRawgGame(RawgGameResource resource)
        {
            var game = new GameMetadata
            {
                RawgId = resource.Id,
                Title = resource.Name,
                CleanTitle = resource.Name.CleanGameTitle(),
                SortTitle = GameTitleNormalizer.Normalize(resource.Name, resource.Id),
                OriginalTitle = resource.Name_Original,
                Overview = resource.Description_Raw ?? StripHtml(resource.Description),
                Website = resource.Website,
                Runtime = resource.Playtime ?? 0,
                Popularity = (float)(resource.Ratings_Count ?? 0),
                Status = GetStatus(resource),
                Year = ParseYear(resource.Released),
                Genres = resource.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                Keywords = resource.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
                Platforms = MapPlatforms(resource.Platforms),
                Developer = resource.Developers?.FirstOrDefault()?.Name,
                Publisher = resource.Publishers?.FirstOrDefault()?.Name,
                Certification = resource.Esrb_Rating?.Name,
                LastInfoSync = DateTime.UtcNow
            };

            // Set release dates
            if (DateTime.TryParse(resource.Released, out var releaseDate))
            {
                game.PhysicalRelease = releaseDate;
                game.DigitalRelease = releaseDate;
            }

            // Set ratings
            if (resource.Rating.HasValue)
            {
                game.Ratings = new Ratings
                {
                    Igdb = new RatingChild
                    {
                        Value = (decimal)(resource.Rating.Value * 20), // Convert 0-5 to 0-100
                        Votes = resource.Ratings_Count ?? 0,
                        Type = RatingType.User
                    }
                };
                game.AggregatedRating = resource.Rating.Value * 20;
                game.AggregatedRatingCount = resource.Ratings_Count;
            }

            // Set Metacritic if available
            if (resource.Metacritic.HasValue)
            {
                game.Ratings ??= new Ratings();
                game.Ratings.Metacritic = new RatingChild
                {
                    Value = resource.Metacritic.Value,
                    Type = RatingType.Critic
                };
            }

            // Set images
            // Note: RAWG only has Background_Image which is landscape-oriented
            // Don't use it as poster since it will look stretched in vertical poster containers
            game.Images = new List<MediaCover.MediaCover>();

            if (!string.IsNullOrEmpty(resource.Background_Image))
            {
                game.Images.Add(new MediaCover.MediaCover
                {
                    RemoteUrl = resource.Background_Image,
                    CoverType = MediaCoverTypes.Fanart
                });
            }

            // Add screenshots
            if (resource.Short_Screenshots != null)
            {
                foreach (var screenshot in resource.Short_Screenshots.Take(5))
                {
                    game.Images.Add(new MediaCover.MediaCover
                    {
                        RemoteUrl = screenshot.Image,
                        CoverType = MediaCoverTypes.Screenshot
                    });
                }
            }

            return game;
        }

        private GameMetadata MapRawgGameBasic(RawgGameResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            var game = new GameMetadata
            {
                RawgId = resource.Id,
                Title = resource.Name,
                CleanTitle = resource.Name.CleanGameTitle(),
                SortTitle = GameTitleNormalizer.Normalize(resource.Name, resource.Id),
                Year = ParseYear(resource.Released),
                Genres = resource.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                Platforms = MapPlatforms(resource.Platforms),
                Status = GetStatus(resource)
            };

            if (DateTime.TryParse(resource.Released, out var releaseDate))
            {
                game.PhysicalRelease = releaseDate;
            }

            if (resource.Rating.HasValue)
            {
                game.Ratings = new Ratings
                {
                    Igdb = new RatingChild
                    {
                        Value = (decimal)(resource.Rating.Value * 20),
                        Votes = resource.Ratings_Count ?? 0,
                        Type = RatingType.User
                    }
                };
            }

            // Note: RAWG only has Background_Image which is landscape-oriented
            // Use it as fanart, not poster, since it will look stretched in vertical poster containers
            if (!string.IsNullOrEmpty(resource.Background_Image))
            {
                game.Images = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover
                    {
                        RemoteUrl = resource.Background_Image,
                        CoverType = MediaCoverTypes.Fanart
                    }
                };
            }

            return game;
        }

        private List<GamePlatform> MapPlatforms(List<RawgPlatformWrapper> platforms)
        {
            if (platforms == null)
            {
                return new List<GamePlatform>();
            }

            return platforms
                .Where(p => p.Platform != null)
                .Select(p => new GamePlatform
                {
                    RawgId = p.Platform.Id,
                    Name = p.Platform.Name,
                    Abbreviation = p.Platform.Slug,
                    Family = MapPlatformFamily(p.Platform.Slug)
                })
                .ToList();
        }

        private PlatformFamily MapPlatformFamily(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return PlatformFamily.Unknown;
            }

            slug = slug.ToLower();

            if (slug.Contains("pc") || slug.Contains("windows"))
            {
                return PlatformFamily.PC;
            }

            if (slug.Contains("linux"))
            {
                return PlatformFamily.Linux;
            }

            if (slug.Contains("mac") || slug.Contains("macos"))
            {
                return PlatformFamily.Mac;
            }

            if (slug.Contains("playstation") || slug.Contains("ps"))
            {
                return PlatformFamily.PlayStation;
            }

            if (slug.Contains("xbox"))
            {
                return PlatformFamily.Xbox;
            }

            if (slug.Contains("nintendo") || slug.Contains("switch") || slug.Contains("wii") || slug.Contains("3ds"))
            {
                return PlatformFamily.Nintendo;
            }

            if (slug.Contains("ios") || slug.Contains("android"))
            {
                return PlatformFamily.Mobile;
            }

            return PlatformFamily.Unknown;
        }

        private GameStatusType GetStatus(RawgGameResource resource)
        {
            if (resource.Tba)
            {
                return GameStatusType.TBA;
            }

            if (!string.IsNullOrEmpty(resource.Released) &&
                DateTime.TryParse(resource.Released, out var releaseDate))
            {
                return releaseDate <= DateTime.UtcNow ? GameStatusType.Released : GameStatusType.Announced;
            }

            return GameStatusType.Released;
        }

        private int ParseYear(string released)
        {
            if (string.IsNullOrEmpty(released))
            {
                return 0;
            }

            if (DateTime.TryParse(released, out var date))
            {
                return date.Year;
            }

            return 0;
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

        /// <summary>
        /// Get suggested/similar games for a game by its RAWG slug or ID.
        /// Returns a list of RAWG IDs for similar games.
        /// </summary>
        public List<int> GetSuggestedGames(string gameSlugOrId, int limit = 10)
        {
            var apiKey = _configService.RawgApiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.Debug("RAWG API key not configured, skipping suggested games lookup");
                return new List<int>();
            }

            var request = new HttpRequestBuilder($"{RawgApiBaseUrl}games/{gameSlugOrId}/suggested")
                .AddQueryParam("key", apiKey)
                .AddQueryParam("page_size", limit)
                .Accept(HttpAccept.Json)
                .Build();

            try
            {
                EnforceRateLimit();
                var response = _httpClient.Get<RawgSearchResult>(request);

                if (response.Resource?.Results == null || !response.Resource.Results.Any())
                {
                    _logger.Debug("No suggested games found for '{0}'", gameSlugOrId);
                    return new List<int>();
                }

                var suggestedIds = response.Resource.Results
                    .Select(g => g.Id)
                    .Take(limit)
                    .ToList();

                _logger.Debug("Found {0} suggested games for '{1}'", suggestedIds.Count, gameSlugOrId);
                return suggestedIds;
            }
            catch (HttpException ex)
            {
                _logger.Debug(ex, "Failed to get suggested games from RAWG for '{0}'", gameSlugOrId);
                return new List<int>();
            }
        }

        /// <summary>
        /// Search RAWG for a game by title and return its slug for use with other endpoints.
        /// </summary>
        public string FindGameSlug(string title)
        {
            var apiKey = _configService.RawgApiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            var request = new HttpRequestBuilder($"{RawgApiBaseUrl}games")
                .AddQueryParam("key", apiKey)
                .AddQueryParam("search", title)
                .AddQueryParam("search_precise", "true")
                .AddQueryParam("page_size", 1)
                .Accept(HttpAccept.Json)
                .Build();

            try
            {
                EnforceRateLimit();
                var response = _httpClient.Get<RawgSearchResult>(request);
                var game = response.Resource?.Results?.FirstOrDefault();
                return game?.Slug;
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to find RAWG slug for '{0}'", title);
                return null;
            }
        }

        private void EnforceRateLimit()
        {
            lock (RateLimitLock)
            {
                var now = DateTime.UtcNow;
                var oneSecondAgo = now.AddSeconds(-1);

                // Remove timestamps older than 1 second
                while (RequestTimestamps.Count > 0 && RequestTimestamps.Peek() < oneSecondAgo)
                {
                    RequestTimestamps.Dequeue();
                }

                // If we've made too many requests, wait
                if (RequestTimestamps.Count >= RateLimitRequestsPerSecond)
                {
                    var oldestRequest = RequestTimestamps.Peek();
                    var waitTime = oldestRequest.AddSeconds(1) - now;
                    if (waitTime.TotalMilliseconds > 0)
                    {
                        _logger.Debug("RAWG rate limit reached, waiting {0}ms", (int)waitTime.TotalMilliseconds);
                        Thread.Sleep((int)waitTime.TotalMilliseconds + 10); // Add 10ms buffer
                    }

                    // Clean up again after waiting
                    now = DateTime.UtcNow;
                    oneSecondAgo = now.AddSeconds(-1);
                    while (RequestTimestamps.Count > 0 && RequestTimestamps.Peek() < oneSecondAgo)
                    {
                        RequestTimestamps.Dequeue();
                    }
                }

                // Record this request
                RequestTimestamps.Enqueue(DateTime.UtcNow);
            }
        }
    }
}
