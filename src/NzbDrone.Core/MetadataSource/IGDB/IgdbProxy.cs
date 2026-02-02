using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.IGDB.Resource;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.MetadataSource.IGDB
{
    /// <summary>
    /// IGDB API Proxy - Handles all communication with the IGDB API.
    /// See https://api-docs.igdb.com/ for API documentation.
    /// </summary>
    public class IgdbProxy : IProvideGameInfo, ISearchForNewGame
    {
        private const string IgdbApiBaseUrl = "https://api.igdb.com/v4/";
        private const int MaxRetries = 3;
        private const int RateLimitRequestsPerSecond = 4;

        private const string GameFields = @"
            fields id, name, slug, summary, storyline, category, status,
            first_release_date, aggregated_rating, aggregated_rating_count,
            rating, rating_count, total_rating, total_rating_count, hypes, follows, url,
            cover.*, screenshots.*, artworks.*, videos.*,
            genres.*, themes.*, game_modes.*, keywords.*,
            platforms.id, platforms.name, platforms.abbreviation, platforms.slug, platforms.category, platforms.generation,
            platforms.platform_family.id, platforms.platform_family.name, platforms.platform_family.slug,
            involved_companies.id, involved_companies.developer, involved_companies.publisher, involved_companies.porting, involved_companies.supporting,
            involved_companies.company.id, involved_companies.company.name, involved_companies.company.slug, involved_companies.company.description,
            game_engines.id, game_engines.name, game_engines.slug, game_engines.description,
            alternative_names.*,
            collection.id, collection.name, collection.slug,
            franchise.id, franchise.name, franchise.slug,
            franchises.id, franchises.name, franchises.slug,
            parent_game.id, parent_game.name,
            dlcs.id, dlcs.name, expansions.id, expansions.name,
            standalone_expansions.id, standalone_expansions.name,
            remakes.id, remakes.name, remasters.id, remasters.name,
            similar_games.id, similar_games.name,
            age_ratings.*, release_dates.*, release_dates.platform.*,
            websites.*, external_games.*;
        ";

        private static readonly HttpStatusCode[] RetryableStatusCodes =
        {
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        };

        // Rate limiting: Track request timestamps to enforce 4 req/sec limit
        private static readonly object RateLimitLock = new object();
        private static readonly Queue<DateTime> RequestTimestamps = new Queue<DateTime>();

        private readonly IHttpClient _httpClient;
        private readonly IIgdbAuthService _authService;
        private readonly IConfigService _configService;
        private readonly IGameService _gameService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly Logger _logger;
        private readonly bool _mockEnabled;
        private readonly string _mockDataPath;

        public IgdbProxy(
            IHttpClient httpClient,
            IIgdbAuthService authService,
            IConfigService configService,
            IGameService gameService,
            IGameMetadataService gameMetadataService,
            IGameTranslationService gameTranslationService,
            Logger logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _configService = configService;
            _gameService = gameService;
            _gameMetadataService = gameMetadataService;
            _gameTranslationService = gameTranslationService;
            _logger = logger;
            _mockEnabled = IsMockMode();
            _mockDataPath = _mockEnabled ? FindMockDataPath() : null;

            if (_mockEnabled)
            {
                _logger.Info("IgdbProxy: Mock mode enabled. Data path: {0}", _mockDataPath ?? "NOT FOUND");
            }
        }

        public GameMetadata GetGameInfoBySteamAppId(int steamAppId)
        {
            // IGDB doesn't support direct Steam lookup; use AggregateGameInfoProxy for Steam games
            return null;
        }

        public GameMetadata GetGameInfoByIgdbId(int igdbId)
        {
            var query = $"{GameFields} where id = {igdbId};";
            var games = ExecuteQuery<IgdbGameResource>("games", query);

            if (games == null || !games.Any())
            {
                throw new GameNotFoundException(igdbId);
            }

            return MapGame(games.First());
        }

        public GameMetadata GetGameBySteamAppId(int steamAppId)
        {
            // IGDB tracks Steam IDs via the external_games endpoint
            // Category 1 = Steam
            var query = $"fields game; where category = 1 & uid = \"{steamAppId}\"; limit 1;";
            var externalGames = ExecuteQuery<IgdbExternalGameResource>("external_games", query);

            if (externalGames == null || !externalGames.Any() || externalGames.First().Game == null)
            {
                _logger.Debug("No IGDB game found for Steam App ID {0}", steamAppId);
                return null;
            }

            var igdbId = externalGames.First().Game.Value;
            var result = GetGameInfoByIgdbId(igdbId);
            if (result != null)
            {
                result.SteamAppId = steamAppId;
            }

            return result;
        }

        public GameCollection GetCollectionInfo(int igdbId)
        {
            var query = $"fields id, name, slug, games.*; where id = {igdbId};";
            var collections = ExecuteQuery<IgdbCollectionResource>("collections", query);

            if (collections == null || !collections.Any())
            {
                throw new GameNotFoundException(igdbId);
            }

            return MapCollection(collections.First());
        }

        public List<GameMetadata> GetBulkGameInfo(List<int> igdbIds)
        {
            if (igdbIds == null || !igdbIds.Any())
            {
                return new List<GameMetadata>();
            }

            var idList = string.Join(",", igdbIds);
            var query = $"{GameFields} where id = ({idList}); limit {igdbIds.Count};";
            var games = ExecuteQuery<IgdbGameResource>("games", query);

            return games?.Select(MapGame).ToList() ?? new List<GameMetadata>();
        }

        public List<GameMetadata> GetBulkGameInfoByIgdbIds(List<int> igdbIds)
        {
            return GetBulkGameInfo(igdbIds);
        }

        public List<GameMetadata> GetBulkGameInfoBySteamAppIds(List<int> steamAppIds)
        {
            // IGDB doesn't support bulk lookup by Steam App IDs directly
            // Use AggregateGameInfoProxy for Steam-based lookups
            return new List<GameMetadata>();
        }

        public List<GameMetadata> GetBulkGameInfoByRawgIds(List<int> rawgIds)
        {
            // IGDB doesn't know RAWG IDs
            return new List<GameMetadata>();
        }

        public HashSet<int> GetChangedGames(DateTime startTime)
        {
            try
            {
                // IGDB uses Unix timestamps
                var unixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

                // Query games directly for updates - IGDB tracks updated_at on games
                var query = $"fields id; where updated_at >= {unixTime}; limit 500;";
                var games = ExecuteQuery<IgdbGameIdResource>("games", query);

                if (games == null || !games.Any())
                {
                    _logger.Debug("No changed games found since {0}", startTime);
                    return new HashSet<int>();
                }

                var changedIds = new HashSet<int>(games.Select(g => g.Id));
                _logger.Debug("Found {0} changed games since {1}", changedIds.Count, startTime);
                return changedIds;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to get changed games from IGDB, returning empty set");
                return new HashSet<int>();
            }
        }

        public List<GameMetadata> GetTrendingGames()
        {
            // Get games with high hype that are releasing soon
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var futureLimit = DateTimeOffset.UtcNow.AddMonths(6).ToUnixTimeSeconds();

            var query = $@"
                {GameFields}
                where first_release_date >= {now} & first_release_date <= {futureLimit} & hypes > 0;
                sort hypes desc;
                limit 50;
            ";

            var games = ExecuteQuery<IgdbGameResource>("games", query);
            return games?.Select(MapGame).ToList() ?? new List<GameMetadata>();
        }

        public List<GameMetadata> GetPopularGames()
        {
            // Get highly rated games from recent years
            var twoYearsAgo = DateTimeOffset.UtcNow.AddYears(-2).ToUnixTimeSeconds();

            var query = $@"
                {GameFields}
                where first_release_date >= {twoYearsAgo} & total_rating_count > 50;
                sort total_rating desc;
                limit 50;
            ";

            var games = ExecuteQuery<IgdbGameResource>("games", query);
            return games?.Select(MapGame).ToList() ?? new List<GameMetadata>();
        }

        public List<int> GetSimilarGames(string title, int igdbId)
        {
            // Get similar games from IGDB's built-in similar_games field
            if (igdbId <= 0)
            {
                return new List<int>();
            }

            var gameInfo = GetGameInfoByIgdbId(igdbId);
            return gameInfo?.IgdbRecommendations ?? new List<int>();
        }

        public List<Game> SearchForNewGame(string title)
        {
            try
            {
                // Check for direct ID lookups
                var lowerTitle = title.ToLower().Trim();

                if (lowerTitle.StartsWith("igdb:") || lowerTitle.StartsWith("igdbid:"))
                {
                    var idStr = lowerTitle.Split(':')[1].Trim();

                    // Support comma-separated IDs like igdb:123,456,789
                    if (idStr.Contains(','))
                    {
                        var results = new List<Game>();
                        var idParts = idStr.Split(',');

                        foreach (var part in idParts)
                        {
                            if (int.TryParse(part.Trim(), out var partId))
                            {
                                try
                                {
                                    var gameLookup = GetGameInfoByIgdbId(partId);
                                    if (gameLookup != null)
                                    {
                                        results.Add(_gameService.FindByIgdbId(gameLookup.IgdbId) ?? new Game { GameMetadata = gameLookup });
                                    }
                                }
                                catch (GameNotFoundException)
                                {
                                    // Skip games not found
                                }
                            }
                        }

                        return results;
                    }

                    if (int.TryParse(idStr, out var igdbId))
                    {
                        try
                        {
                            var gameLookup = GetGameInfoByIgdbId(igdbId);
                            return gameLookup == null
                                ? new List<Game>()
                                : new List<Game> { _gameService.FindByIgdbId(gameLookup.IgdbId) ?? new Game { GameMetadata = gameLookup } };
                        }
                        catch (GameNotFoundException)
                        {
                            return new List<Game>();
                        }
                    }
                }

                if (lowerTitle.StartsWith("steam:") || lowerTitle.StartsWith("steamid:"))
                {
                    var idStr = lowerTitle.Split(':')[1].Trim();
                    if (int.TryParse(idStr, out var steamAppId))
                    {
                        var gameLookup = GetGameBySteamAppId(steamAppId);
                        if (gameLookup != null)
                        {
                            return new List<Game> { _gameService.FindByIgdbId(gameLookup.IgdbId) ?? new Game { GameMetadata = gameLookup } };
                        }

                        return new List<Game>();
                    }
                }

                // Parse the title for year information
                var parserResult = Parser.Parser.ParseGameTitle(title, true);
                var searchTitle = parserResult?.PrimaryGameTitle ?? title;

                // Use IGDB search endpoint
                // Include null category for games without category set (e.g., Metroid Prime 4)
                var query = $@"
                    {GameFields}
                    search ""{EscapeQuery(searchTitle)}"";
                    where category = null | category = (0, 2, 4, 8, 9, 10, 11);
                    limit 20;
                ";

                var games = ExecuteQuery<IgdbGameResource>("games", query);

                if (games == null || !games.Any())
                {
                    return new List<Game>();
                }

                return games.Select(g =>
                {
                    var existingGame = _gameService.FindByIgdbId(g.Id);
                    if (existingGame != null)
                    {
                        existingGame.GameMetadata.Value.Translations = _gameTranslationService.GetAllTranslationsForGameMetadata(existingGame.GameMetadataId);
                        return existingGame;
                    }

                    return new Game { GameMetadata = MapGame(g) };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error searching for game: {0}", CleanseLogMessage.SanitizeLogParam(title));
                throw new Exceptions.SearchFailedException($"Search for '{title}' failed: {ex.Message}");
            }
        }

        public GameMetadata MapGameToIgdbGame(GameMetadata game)
        {
            try
            {
                if (game.IgdbId > 0)
                {
                    var existing = _gameMetadataService.FindByIgdbId(game.IgdbId);
                    if (existing != null)
                    {
                        return existing;
                    }

                    return GetGameInfoByIgdbId(game.IgdbId);
                }

                // Search by title
                var yearStr = game.Year > 1900 ? $" {game.Year}" : "";
                var searchResult = SearchForNewGame(game.Title + yearStr).FirstOrDefault();

                return searchResult?.GameMetadata?.Value;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Couldn't map game {0} to IGDB. It will not be added.", game.Title);
                return null;
            }
        }

        private List<T> ExecuteQuery<T>(string endpoint, string query)
            where T : class
        {
            if (_mockEnabled)
            {
                return LoadMockData<T>(query);
            }

            var accessToken = _authService.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.Error("No IGDB access token available. Please configure IGDB credentials.");
                return new List<T>();
            }

            for (var attempt = 0; attempt <= MaxRetries; attempt++)
            {
                // Enforce rate limit before making request
                EnforceRateLimit();

                var requestBuilder = new HttpRequestBuilder($"{IgdbApiBaseUrl}{endpoint}")
                    .Post()
                    .SetHeader("Client-ID", _authService.ClientId)
                    .SetHeader("Authorization", $"Bearer {accessToken}");

                var request = requestBuilder.Build();
                request.Headers.ContentType = "text/plain";
                request.SetContent(query);
                request.AllowAutoRedirect = true;
                request.SuppressHttpError = true;

                HttpResponse<List<T>> response;

                try
                {
                    response = _httpClient.Post<List<T>>(request);
                }
                catch (Exception ex) when (attempt < MaxRetries && IsTransientException(ex))
                {
                    var delay = (int)Math.Pow(2, attempt) * 1000;
                    _logger.Warn("IGDB request failed (attempt {0}/{1}), retrying in {2}ms: {3}", attempt + 1, MaxRetries + 1, delay, ex.Message);
                    Thread.Sleep(delay);
                    continue;
                }

                if (response.HasHttpError)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new List<T>();
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _logger.Error("IGDB authentication failed. Please check your credentials.");
                        throw new HttpException(request, response);
                    }

                    if (attempt < MaxRetries && RetryableStatusCodes.Contains(response.StatusCode))
                    {
                        var delay = (int)Math.Pow(2, attempt) * 1000;
                        _logger.Warn("IGDB API returned {0} (attempt {1}/{2}), retrying in {3}ms", response.StatusCode, attempt + 1, MaxRetries + 1, delay);
                        Thread.Sleep(delay);
                        continue;
                    }

                    _logger.Error("IGDB API error: {0} - {1}", response.StatusCode, response.Content);
                    throw new HttpException(request, response);
                }

                return response.Resource ?? new List<T>();
            }

            return new List<T>();
        }

        private static bool IsTransientException(Exception ex)
        {
            return ex is System.Net.Http.HttpRequestException ||
                   ex is System.Threading.Tasks.TaskCanceledException ||
                   ex is TimeoutException;
        }

        /// <summary>
        /// Enforces rate limiting of 4 requests per second for IGDB free tier.
        /// </summary>
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

                // If we've made 4+ requests in the last second, wait
                if (RequestTimestamps.Count >= RateLimitRequestsPerSecond)
                {
                    var oldestRequest = RequestTimestamps.Peek();
                    var waitTime = oldestRequest.AddSeconds(1) - now;
                    if (waitTime.TotalMilliseconds > 0)
                    {
                        _logger.Debug("IGDB rate limit reached, waiting {0}ms", (int)waitTime.TotalMilliseconds);
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

        private List<T> LoadMockData<T>(string query)
            where T : class
        {
            if (string.IsNullOrEmpty(_mockDataPath))
            {
                _logger.Warn("Mock mode enabled but no mock data path found");
                return new List<T>();
            }

            // Parse query for ID lookup: "where id = X" or "where id = (X, Y, Z)"
            var idMatch = Regex.Match(query, @"where\s+id\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (idMatch.Success)
            {
                var igdbId = idMatch.Groups[1].Value;
                var content = LoadMockFile($"igdb_game_{igdbId}.json");
                if (content != null)
                {
                    _logger.Debug("Mock: Loaded game data for IGDB ID {0}", igdbId);
                    return JsonConvert.DeserializeObject<List<T>>(content) ?? new List<T>();
                }

                _logger.Debug("Mock: No data file for IGDB ID {0}", igdbId);
                return new List<T>();
            }

            // Parse query for multi-ID lookup: "where id = (X, Y, Z)"
            var multiIdMatch = Regex.Match(query, @"where\s+id\s*=\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
            if (multiIdMatch.Success)
            {
                var results = new List<T>();
                var ids = multiIdMatch.Groups[1].Value.Split(',');
                foreach (var id in ids)
                {
                    var trimmedId = id.Trim();
                    var content = LoadMockFile($"igdb_game_{trimmedId}.json");
                    if (content != null)
                    {
                        var items = JsonConvert.DeserializeObject<List<T>>(content);
                        if (items != null)
                        {
                            results.AddRange(items);
                        }
                    }
                }

                return results;
            }

            // Parse query for search: search "term"
            var searchMatch = Regex.Match(query, @"search\s+""([^""]+)""", RegexOptions.IgnoreCase);
            if (searchMatch.Success)
            {
                var rawTerm = searchMatch.Groups[1].Value;
                var searchTerm = rawTerm.ToLowerInvariant().Replace(" ", "").Replace("-", "");

                _logger.Debug("Mock: Search for '{0}' (normalized: '{1}')", rawTerm, searchTerm);

                // Try exact match first
                var content = LoadMockFile($"igdb_search_{searchTerm}.json");
                if (content != null)
                {
                    return JsonConvert.DeserializeObject<List<T>>(content) ?? new List<T>();
                }

                // Try partial matches
                var files = Directory.GetFiles(_mockDataPath, "igdb_search_*.json");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileTerm = fileName.Replace("igdb_search_", "").ToLowerInvariant();
                    if (searchTerm.Contains(fileTerm) || fileTerm.Contains(searchTerm))
                    {
                        content = File.ReadAllText(file);
                        return JsonConvert.DeserializeObject<List<T>>(content) ?? new List<T>();
                    }
                }

                _logger.Debug("Mock: No search results for '{0}'", rawTerm);
                return new List<T>();
            }

            // Parse query for external_games (Steam ID lookup)
            var uidMatch = Regex.Match(query, @"uid\s*=\s*""(\d+)""", RegexOptions.IgnoreCase);
            if (uidMatch.Success)
            {
                var steamId = uidMatch.Groups[1].Value;
                var content = LoadMockFile($"igdb_external_{steamId}.json");
                if (content != null)
                {
                    return JsonConvert.DeserializeObject<List<T>>(content) ?? new List<T>();
                }

                return new List<T>();
            }

            _logger.Debug("Mock: Could not parse query, returning empty: {0}",
                query.Length > 100 ? query.Substring(0, 100) : query);
            return new List<T>();
        }

        private string LoadMockFile(string fileName)
        {
            if (string.IsNullOrEmpty(_mockDataPath))
            {
                return null;
            }

            var filePath = Path.Combine(_mockDataPath, fileName);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }

            return null;
        }

        private static bool IsMockMode()
        {
            var envValue = Environment.GetEnvironmentVariable("GAMARR_MOCK_METADATA")
                        ?? Environment.GetEnvironmentVariable("gamarr_mock_metadata");

            return !string.IsNullOrEmpty(envValue) &&
                   (envValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    envValue.Equals("1", StringComparison.OrdinalIgnoreCase));
        }

        private static string FindMockDataPath()
        {
            var envPath = Environment.GetEnvironmentVariable("GAMARR_MOCK_DATA_PATH")
                       ?? Environment.GetEnvironmentVariable("gamarr_mock_data_path");

            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return envPath;
            }

            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var possiblePaths = new[]
            {
                Path.Combine(appDir, "MockData"),
                Path.Combine(appDir, "Files", "MockData"),
                Path.Combine(appDir, "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(appDir, "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(appDir, "..", "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(appDir, "..", "..", "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(appDir, "..", "..", "..", "..", "src", "NzbDrone.Core.Test", "Files", "MockData")
            };

            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // Try to find it from the source root
            var current = appDir;
            while (!string.IsNullOrEmpty(current))
            {
                if (Directory.Exists(Path.Combine(current, ".git")) ||
                    File.Exists(Path.Combine(current, "Gamarr.sln")))
                {
                    var srcPath = Path.Combine(current, "src", "NzbDrone.Core.Test", "Files", "MockData");
                    if (Directory.Exists(srcPath))
                    {
                        return srcPath;
                    }

                    break;
                }

                var parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            return null;
        }

        private GameMetadata MapGame(IgdbGameResource resource)
        {
            var game = new GameMetadata
            {
                IgdbId = resource.Id,
                IgdbSlug = resource.Slug,
                Title = resource.Name,
                OriginalTitle = resource.Name,
                CleanTitle = resource.Name.CleanGameTitle(),
                SortTitle = GameTitleNormalizer.Normalize(resource.Name, resource.Id),
                CleanOriginalTitle = resource.Name.CleanGameTitle(),
                Overview = resource.Summary ?? resource.Storyline,
                GameType = (GameType)resource.Category,
                Website = resource.Url
            };

            // Release date
            if (resource.FirstReleaseDate.HasValue)
            {
                var releaseDate = DateTimeOffset.FromUnixTimeSeconds(resource.FirstReleaseDate.Value).DateTime;
                game.DigitalRelease = releaseDate;
                game.Year = releaseDate.Year;
            }

            // Ratings - IGDB returns 0-100 scale, keep it as-is for display
            game.Ratings = new Ratings();
            if (resource.TotalRating.HasValue)
            {
                game.Ratings.Igdb = new RatingChild
                {
                    Value = (decimal)resource.TotalRating.Value,
                    Votes = resource.TotalRatingCount ?? 0
                };
            }

            game.AggregatedRating = resource.AggregatedRating;
            game.AggregatedRatingCount = resource.AggregatedRatingCount;
            game.Popularity = resource.Follows ?? resource.Hypes ?? 0;

            // Images
            game.Images = new List<MediaCover.MediaCover>();
            if (resource.Cover != null)
            {
                game.Images.Add(new MediaCover.MediaCover
                {
                    CoverType = MediaCoverTypes.Poster,
                    RemoteUrl = resource.Cover.GetImageUrl("cover_big")
                });
            }

            if (resource.Artworks != null)
            {
                foreach (var artwork in resource.Artworks.Take(3))
                {
                    game.Images.Add(new MediaCover.MediaCover
                    {
                        CoverType = MediaCoverTypes.Fanart,
                        RemoteUrl = artwork.GetImageUrl("1080p")
                    });
                }
            }

            if (resource.Screenshots != null)
            {
                foreach (var screenshot in resource.Screenshots.Take(5))
                {
                    game.Images.Add(new MediaCover.MediaCover
                    {
                        CoverType = MediaCoverTypes.Screenshot,
                        RemoteUrl = screenshot.GetImageUrl("screenshot_big")
                    });
                }
            }

            // Genres
            game.Genres = resource.Genres?.Select(g => g.Name).ToList() ?? new List<string>();

            // Themes
            game.Themes = resource.Themes?.Select(t => t.Name).ToList() ?? new List<string>();

            // Game modes
            game.GameModes = resource.GameModes?.Select(m => m.Name).ToList() ?? new List<string>();

            // Keywords
            game.Keywords = resource.Keywords?.Select(k => k.Name).ToList() ?? new List<string>();

            // Platforms
            game.Platforms = resource.Platforms?.Select(p => new GamePlatform
            {
                IgdbId = p.Id,
                Name = p.Name,
                Abbreviation = p.Abbreviation,
                Slug = p.Slug,
                Category = p.Category.HasValue ? (GamePlatformCategory)p.Category.Value : GamePlatformCategory.Console,
                Generation = p.Generation,
                Family = GamePlatform.MapPlatformFamily(p.PlatformFamily?.Id)
            }).ToList() ?? new List<GamePlatform>();

            // Companies (Developer/Publisher)
            if (resource.InvolvedCompanies != null)
            {
                var developers = resource.InvolvedCompanies
                    .Where(ic => ic.Developer && ic.Company != null)
                    .Select(ic => ic.Company.Name)
                    .ToList();

                var publishers = resource.InvolvedCompanies
                    .Where(ic => ic.Publisher && ic.Company != null)
                    .Select(ic => ic.Company.Name)
                    .ToList();

                game.Developer = string.Join(", ", developers);
                game.Publisher = string.Join(", ", publishers);
                game.Studio = game.Developer;
            }

            // Game engine
            if (resource.GameEngines != null && resource.GameEngines.Any())
            {
                game.GameEngine = resource.GameEngines.First().Name;
            }

            // Alternative titles
            if (resource.AlternativeNames != null)
            {
                game.AlternativeTitles = resource.AlternativeNames.Select(an => new AlternativeTitle
                {
                    Title = an.Name,
                    CleanTitle = an.Name.CleanGameTitle(),
                    SourceType = SourceType.Igdb
                }).ToList();
            }

            // Collection/Franchise
            if (resource.Collection != null)
            {
                game.CollectionIgdbId = resource.Collection.Id;
                game.CollectionTitle = resource.Collection.Name;
            }
            else if (resource.Franchise != null)
            {
                game.CollectionIgdbId = resource.Franchise.Id;
                game.CollectionTitle = resource.Franchise.Name;
            }

            // Parent game (for DLCs)
            if (resource.ParentGame != null)
            {
                game.ParentGameId = resource.ParentGame.Id;
            }

            // DLCs and expansions
            var dlcIds = new List<int>();
            var dlcReferences = new List<DlcReference>();
            if (resource.Dlcs != null)
            {
                dlcIds.AddRange(resource.Dlcs.Select(d => d.Id));
                dlcReferences.AddRange(resource.Dlcs.Select(d => new DlcReference(d.Id, d.Name)));
            }

            if (resource.Expansions != null)
            {
                dlcIds.AddRange(resource.Expansions.Select(e => e.Id));
                dlcReferences.AddRange(resource.Expansions.Select(e => new DlcReference(e.Id, e.Name)));
            }

            if (resource.StandaloneExpansions != null)
            {
                dlcIds.AddRange(resource.StandaloneExpansions.Select(s => s.Id));
                dlcReferences.AddRange(resource.StandaloneExpansions.Select(s => new DlcReference(s.Id, s.Name)));
            }

            game.IgdbDlcIds = dlcIds;
            game.DlcReferences = dlcReferences;

            // Similar games as recommendations
            if (resource.SimilarGames != null)
            {
                game.IgdbRecommendations = resource.SimilarGames.Select(s => s.Id).ToList();
            }

            // Videos (YouTube trailer)
            if (resource.Videos != null && resource.Videos.Any())
            {
                game.YouTubeTrailerId = resource.Videos.First().VideoId;
            }

            // Age rating / Certification
            if (resource.AgeRatings != null)
            {
                // Only use known rating organizations (1-7)
                var knownRatings = resource.AgeRatings.Where(ar => ar.Category >= 1 && ar.Category <= 7).ToList();
                var esrbRating = knownRatings.FirstOrDefault(ar => ar.Category == 1);
                var pegiRating = knownRatings.FirstOrDefault(ar => ar.Category == 2);

                var selectedRating = esrbRating ?? pegiRating ?? knownRatings.FirstOrDefault();
                if (selectedRating != null)
                {
                    game.Certification = $"{selectedRating.GetRatingOrganization()}-{selectedRating.Rating}";
                }
            }

            // Status
            game.Status = MapStatus(resource.Status);

            return game;
        }

        private GameCollection MapCollection(IgdbCollectionResource resource)
        {
            return new GameCollection
            {
                IgdbId = resource.Id,
                Title = resource.Name,
                CleanTitle = resource.Name.CleanGameTitle(),
                SortTitle = Parser.Parser.NormalizeTitle(resource.Name),
                Games = resource.Games?.Select(g => MapGame(g)).ToList() ?? new List<GameMetadata>()
            };
        }

        private GameStatusType MapStatus(int? igdbStatus)
        {
            // IGDB Status: 0=Released, 2=Alpha, 3=Beta, 4=Early Access, 5=Offline, 6=Cancelled, 7=Rumored, 8=Delisted
            return igdbStatus switch
            {
                0 => GameStatusType.Released,
                2 => GameStatusType.EarlyAccess,
                3 => GameStatusType.EarlyAccess,
                4 => GameStatusType.EarlyAccess,
                5 => GameStatusType.Released,
                6 => GameStatusType.Deleted,
                7 => GameStatusType.Announced,
                8 => GameStatusType.Released,
                _ => GameStatusType.Announced
            };
        }

        private string EscapeQuery(string input)
        {
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", " ")
                .Replace("\r", " ");
        }
    }
}
