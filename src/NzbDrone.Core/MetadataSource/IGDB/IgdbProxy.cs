using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Credits;
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
    /// <remarks>
    /// TODO: Implement rate limiting (4 requests per second for free tier).
    /// TODO: Add webhook support for real-time updates.
    /// TODO: Implement multi-query for batch requests.
    /// </remarks>
    public class IgdbProxy : IProvideGameInfo, ISearchForNewGame
    {
        private const string IgdbApiBaseUrl = "https://api.igdb.com/v4/";

        private const string GameFields = @"
            fields id, name, slug, summary, storyline, category, status,
            first_release_date, aggregated_rating, aggregated_rating_count,
            rating, rating_count, total_rating, total_rating_count, hypes, follows, url,
            cover.*, screenshots.*, artworks.*, videos.*,
            genres.*, themes.*, game_modes.*, keywords.*,
            platforms.*, platforms.platform_family.*,
            involved_companies.*, involved_companies.company.*,
            game_engines.*, alternative_names.*,
            collection.*, franchise.*, franchises.*,
            parent_game.id, parent_game.name,
            dlcs.id, dlcs.name, expansions.id, expansions.name,
            standalone_expansions.id, standalone_expansions.name,
            remakes.id, remakes.name, remasters.id, remasters.name,
            similar_games.id, similar_games.name,
            age_ratings.*, release_dates.*, release_dates.platform.*,
            websites.*, external_games.*;
        ";

        private readonly IHttpClient _httpClient;
        private readonly IIgdbAuthService _authService;
        private readonly IConfigService _configService;
        private readonly IGameService _gameService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly Logger _logger;

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
        }

        public Tuple<GameMetadata, List<Credit>> GetGameInfoBySteamAppId(int steamAppId)
        {
            // IGDB doesn't support direct Steam lookup; use AggregateGameInfoProxy for Steam games
            return new Tuple<GameMetadata, List<Credit>>(null, new List<Credit>());
        }

        public Tuple<GameMetadata, List<Credit>> GetGameInfo(int igdbId)
        {
            var query = $"{GameFields} where id = {igdbId};";
            var games = ExecuteQuery<IgdbGameResource>("games", query);

            if (games == null || !games.Any())
            {
                throw new GameNotFoundException(igdbId);
            }

            var game = MapGame(games.First());

            // TODO: IGDB has involved companies but not traditional cast/crew, return empty credits for now
            var credits = new List<Credit>();

            return new Tuple<GameMetadata, List<Credit>>(game, credits);
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
            var result = GetGameInfo(igdbId);
            if (result?.Item1 != null)
            {
                result.Item1.SteamAppId = steamAppId;
            }

            return result?.Item1;
        }

        public GameMetadata GetGameByImdbId(string imdbId)
        {
            // IGDB doesn't use IMDb IDs directly
            _logger.Warn("IGDB does not support IMDb ID lookup. Use Steam App ID or IGDB ID instead.");
            return null;
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

        public HashSet<int> GetChangedGames(DateTime startTime)
        {
            // IGDB uses Unix timestamps
            var unixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
            var query = $"fields game; where updated_at >= {unixTime}; limit 500;";

            // TODO: Use the change endpoint or updated_at field properly
            // For now, return empty set - games don't change as frequently as we need to poll
            _logger.Debug("GetChangedGames not fully implemented for IGDB. Returning empty set.");
            return new HashSet<int>();
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

        public List<Game> SearchForNewGame(string title)
        {
            try
            {
                // Check for direct ID lookups
                var lowerTitle = title.ToLower().Trim();

                if (lowerTitle.StartsWith("igdb:") || lowerTitle.StartsWith("igdbid:"))
                {
                    var idStr = lowerTitle.Split(':')[1].Trim();
                    if (int.TryParse(idStr, out var igdbId))
                    {
                        try
                        {
                            var gameLookup = GetGameInfo(igdbId).Item1;
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

                // Parse the title for year information
                var parserResult = Parser.Parser.ParseGameTitle(title, true);
                var searchTitle = parserResult?.PrimaryGameTitle ?? title;

                // Use IGDB search endpoint
                var query = $@"
                    {GameFields}
                    search ""{EscapeQuery(searchTitle)}"";
                    where category = (0, 2, 4, 8, 9, 10, 11);
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
                _logger.Error(ex, "Error searching for game: {0}", title);
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

                    return GetGameInfo(game.IgdbId).Item1;
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
            var accessToken = _authService.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.Error("No IGDB access token available. Please configure IGDB credentials.");

                // TODO: Return mock data or throw appropriate exception
                return new List<T>();
            }

            var requestBuilder = new HttpRequestBuilder($"{IgdbApiBaseUrl}{endpoint}")
                .Post()
                .SetHeader("Client-ID", _authService.ClientId)
                .SetHeader("Authorization", $"Bearer {accessToken}");

            var request = requestBuilder.Build();
            request.Headers.ContentType = "text/plain";
            request.SetContent(query);
            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Post<List<T>>(request);

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

                _logger.Error("IGDB API error: {0} - {1}", response.StatusCode, response.Content);
                throw new HttpException(request, response);
            }

            return response.Resource ?? new List<T>();
        }

        private GameMetadata MapGame(IgdbGameResource resource)
        {
            var game = new GameMetadata
            {
                IgdbId = resource.Id,
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

            // Ratings
            game.Ratings = new Ratings();
            if (resource.TotalRating.HasValue)
            {
                game.Ratings.Igdb = new RatingChild
                {
                    Value = (decimal)(resource.TotalRating.Value / 10),
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
            if (resource.Dlcs != null)
            {
                dlcIds.AddRange(resource.Dlcs.Select(d => d.Id));
            }

            if (resource.Expansions != null)
            {
                dlcIds.AddRange(resource.Expansions.Select(e => e.Id));
            }

            if (resource.StandaloneExpansions != null)
            {
                dlcIds.AddRange(resource.StandaloneExpansions.Select(s => s.Id));
            }

            game.DlcIds = dlcIds;

            // Similar games as recommendations
            if (resource.SimilarGames != null)
            {
                game.Recommendations = resource.SimilarGames.Select(s => s.Id).ToList();
            }

            // Videos (YouTube trailer)
            if (resource.Videos != null && resource.Videos.Any())
            {
                game.YouTubeTrailerId = resource.Videos.First().VideoId;
            }

            // Age rating / Certification
            if (resource.AgeRatings != null)
            {
                var esrbRating = resource.AgeRatings.FirstOrDefault(ar => ar.Category == 1);
                var pegiRating = resource.AgeRatings.FirstOrDefault(ar => ar.Category == 2);

                var selectedRating = esrbRating ?? pegiRating ?? resource.AgeRatings.FirstOrDefault();
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
