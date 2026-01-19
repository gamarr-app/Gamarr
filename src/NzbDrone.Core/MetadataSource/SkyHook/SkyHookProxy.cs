using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.SkyHook.Resource;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Credits;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.MetadataSource.SkyHook
{
    /// <summary>
    /// Legacy metadata proxy that uses the GamarrMetadata API.
    /// TODO: This class is deprecated and will be replaced by IgdbProxy.
    /// The IgdbProxy provides direct IGDB API integration with proper OAuth.
    /// For now, this class is kept as a fallback until IgdbProxy is fully tested.
    /// </summary>
    [Obsolete("Use NzbDrone.Core.MetadataSource.IGDB.IgdbProxy instead")]
    public class SkyHookProxy : IProvideGameInfo, ISearchForNewGame
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        private readonly IHttpRequestBuilderFactory _gamarrMetadata;
        private readonly IConfigService _configService;
        private readonly IGameService _gameService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly IGameTranslationService _gameTranslationService;

        public SkyHookProxy(IHttpClient httpClient,
            IGamarrCloudRequestBuilder requestBuilder,
            IConfigService configService,
            IGameService gameService,
            IGameMetadataService gameMetadataService,
            IGameTranslationService gameTranslationService,
            Logger logger)
        {
            _httpClient = httpClient;
            _gamarrMetadata = requestBuilder.GamarrMetadata;
            _configService = configService;
            _gameService = gameService;
            _gameMetadataService = gameMetadataService;
            _gameTranslationService = gameTranslationService;

            _logger = logger;
        }

        public HashSet<int> GetChangedGames(DateTime startTime)
        {
            // Round down to the hour to ensure we cover gap and don't kill cache every call
            var cacheAdjustedStart = startTime.AddMinutes(-15);
            var startDate = cacheAdjustedStart.Date.AddHours(cacheAdjustedStart.Hour).ToString("s");

            var request = _gamarrMetadata.Create()
                .SetSegment("route", "game/changed")
                .AddQueryParam("since", startDate)
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get<List<int>>(request);

            return new HashSet<int>(response.Resource);
        }

        public List<GameMetadata> GetTrendingGames()
        {
            var request = _gamarrMetadata.Create()
                .SetSegment("route", "list/igdb/trending")
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get<List<GameResource>>(request);

            return response.Resource.DistinctBy(x => x.IgdbId).Select(MapGame).ToList();
        }

        public List<GameMetadata> GetPopularGames()
        {
            var request = _gamarrMetadata.Create()
                .SetSegment("route", "list/igdb/popular")
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get<List<GameResource>>(request);

            return response.Resource.DistinctBy(x => x.IgdbId).Select(MapGame).ToList();
        }

        public Tuple<GameMetadata, List<Credit>> GetGameInfo(int igdbId)
        {
            var httpRequest = _gamarrMetadata.Create()
                                             .SetSegment("route", "game")
                                             .Resource(igdbId.ToString())
                                             .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<GameResource>(httpRequest);

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new GameNotFoundException(igdbId);
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var credits = new List<Credit>();
            credits.AddRange(httpResponse.Resource.Credits.Cast.Select(MapCast));
            credits.AddRange(httpResponse.Resource.Credits.Crew.Select(MapCrew));

            var game = MapGame(httpResponse.Resource);

            return new Tuple<GameMetadata, List<Credit>>(game, credits.ToList());
        }

        public GameCollection GetCollectionInfo(int igdbId)
        {
            var httpRequest = _gamarrMetadata.Create()
                                             .SetSegment("route", "game/collection")
                                             .Resource(igdbId.ToString())
                                             .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<CollectionResource>(httpRequest);

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new GameNotFoundException(igdbId);
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var collection = MapCollection(httpResponse.Resource);

            return collection;
        }

        public List<GameMetadata> GetBulkGameInfo(List<int> igdbIds)
        {
            var httpRequest = _gamarrMetadata.Create()
                                             .SetSegment("route", "game/bulk")
                                             .Build();

            httpRequest.Headers.ContentType = "application/json";

            httpRequest.SetContent(igdbIds.ToJson());
            httpRequest.ContentSummary = igdbIds.ToJson(Formatting.None);

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Post<List<GameResource>>(httpRequest);

            if (httpResponse.HasHttpError || httpResponse.Resource.Count == 0)
            {
                throw new HttpException(httpRequest, httpResponse);
            }

            var games = httpResponse.Resource.Select(MapGame).ToList();

            return games;
        }

        public GameMetadata GetGameByImdbId(string imdbId)
        {
            imdbId = Parser.Parser.NormalizeImdbId(imdbId);

            if (imdbId == null)
            {
                return null;
            }

            var httpRequest = _gamarrMetadata.Create()
                                             .SetSegment("route", "game/imdb")
                                             .Resource(imdbId.ToString())
                                             .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<List<GameResource>>(httpRequest);

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new GameNotFoundException(imdbId);
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var game = httpResponse.Resource.SelectList(MapGame).FirstOrDefault();

            return game;
        }

        public GameMetadata MapGame(GameResource resource)
        {
            var game = new GameMetadata();
            var altTitles = new List<AlternativeTitle>();

            game.IgdbId = resource.IgdbId;
            game.ImdbId = resource.ImdbId;
            game.Title = resource.Title;
            game.OriginalTitle = resource.OriginalTitle;
            game.CleanTitle = resource.Title.CleanGameTitle();
            game.SortTitle = GameTitleNormalizer.Normalize(resource.Title, resource.IgdbId);
            game.CleanOriginalTitle = resource.OriginalTitle.CleanGameTitle();
            game.Overview = resource.Overview;

            game.AlternativeTitles.AddRange(resource.AlternativeTitles.Select(MapAlternativeTitle));

            game.Translations.AddRange(resource.Translations.Select(MapTranslation));

            game.OriginalLanguage = IsoLanguages.Find(resource.OriginalLanguage.ToLower())?.Language ?? Language.English;

            game.Website = resource.Homepage;
            game.InDevelopment = resource.InCinema;
            game.PhysicalRelease = resource.PhysicalRelease;
            game.DigitalRelease = resource.DigitalRelease;

            game.Year = resource.Year;

            // If the premier differs from the IGDB year, use it as a secondary year.
            if (resource.Premier.HasValue && resource.Premier?.Year != game.Year)
            {
                game.SecondaryYear = resource.Premier?.Year;
            }

            if (resource.Runtime != null)
            {
                game.Runtime = resource.Runtime.Value;
            }

            if (resource.Popularity != null)
            {
                game.Popularity = resource.Popularity.Value;
            }

            var certificationCountry = _configService.CertificationCountry.ToString();

            game.Certification = resource.Certifications.FirstOrDefault(m => m.Country == certificationCountry)?.Certification;
            game.Ratings = MapRatings(resource.GameRatings) ?? new Ratings();

            game.IgdbId = resource.IgdbId;
            game.Genres = resource.Genres ?? new List<string>();
            game.Keywords = resource.Keywords ?? new List<string>();
            game.Images = resource.Images.Select(MapImage).ToList();

            game.Recommendations = resource.Recommendations?.Select(r => r.IgdbId).ToList() ?? new List<int>();

            // Workaround due to metadata change until cache cleans up
            if (game.Ratings.Igdb == null)
            {
                var igdbRating = resource.Ratings.FirstOrDefault();
                game.Ratings.Igdb = new RatingChild
                {
                    Votes = igdbRating.Count,
                    Value = igdbRating.Value
                };
            }

            var now = DateTime.UtcNow;

            game.Status = GameStatusType.Announced;

            if (resource.InCinema.HasValue && now > resource.InCinema)
            {
                game.Status = GameStatusType.InDevelopment;

                if (!resource.PhysicalRelease.HasValue && !resource.DigitalRelease.HasValue && now > resource.InCinema.Value.AddDays(90))
                {
                    game.Status = GameStatusType.Released;
                }
            }

            if (resource.PhysicalRelease.HasValue && now >= resource.PhysicalRelease)
            {
                game.Status = GameStatusType.Released;
            }

            if (resource.DigitalRelease.HasValue && now >= resource.DigitalRelease)
            {
                game.Status = GameStatusType.Released;
            }

            game.YouTubeTrailerId = resource.YoutubeTrailerId;
            game.Studio = resource.Studio;

            if (resource.Collection != null)
            {
                game.CollectionIgdbId = resource.Collection.IgdbId;
                game.CollectionTitle = resource.Collection.Name;
            }

            return game;
        }

        private string StripTrailingTheFromTitle(string title)
        {
            if (title.EndsWith(",the"))
            {
                title = title.Substring(0, title.Length - 4);
            }
            else if (title.EndsWith(", the"))
            {
                title = title.Substring(0, title.Length - 5);
            }

            return title;
        }

        public GameMetadata MapGameToIgdbGame(GameMetadata game)
        {
            try
            {
                var newGame = game;

                if (game.IgdbId > 0)
                {
                    newGame = _gameMetadataService.FindByIgdbId(game.IgdbId);

                    if (newGame != null)
                    {
                        return newGame;
                    }

                    newGame = GetGameInfo(game.IgdbId).Item1;
                }
                else if (game.ImdbId.IsNotNullOrWhiteSpace())
                {
                    newGame = _gameMetadataService.FindByImdbId(Parser.Parser.NormalizeImdbId(game.ImdbId));

                    if (newGame != null)
                    {
                        return newGame;
                    }

                    newGame = GetGameByImdbId(game.ImdbId);
                }
                else
                {
                    var yearStr = "";
                    if (game.Year > 1900)
                    {
                        yearStr = $" {game.Year}";
                    }

                    var newGameObject = SearchForNewGame(game.Title + yearStr).FirstOrDefault();

                    if (newGameObject == null)
                    {
                        newGame = null;
                    }
                    else
                    {
                        newGame = newGameObject.GameMetadata;
                    }
                }

                if (newGame == null)
                {
                    _logger.Warn("Couldn't map game {0} to a game on The Game DB. It will not be added :(", game.Title);
                    return null;
                }

                return newGame;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Couldn't map game {0} to a game on The Game DB. It will not be added :(", game.Title);
                return null;
            }
        }

        public List<Game> SearchForNewGame(string title)
        {
            try
            {
                var match = new Regex(@"\bimdb\.com/title/(tt\d{7,})\b", RegexOptions.IgnoreCase).Match(title);

                if (match.Success)
                {
                    title = "imdb:" + match.Groups[1].Value;
                }
                else
                {
                    match = new Regex(@"\bthegamedb\.org/game/(\d+)\b", RegexOptions.IgnoreCase).Match(title);

                    if (match.Success)
                    {
                        title = "igdb:" + match.Groups[1].Value;
                    }
                }

                var lowerTitle = title.ToLower();

                lowerTitle = lowerTitle.Replace(".", "");

                var parserTitle = lowerTitle;

                var parserResult = Parser.Parser.ParseGameTitle(title, true);

                var yearTerm = "";

                if (parserResult != null && parserResult.PrimaryGameTitle != title)
                {
                    // Parser found something interesting!
                    parserTitle = parserResult.PrimaryGameTitle.ToLower().Replace(".", " "); // TODO Update so not every period gets replaced (e.g. R.I.P.D.)
                    if (parserResult.Year > 1800)
                    {
                        yearTerm = parserResult.Year.ToString();
                    }

                    if (parserResult.ImdbId.IsNotNullOrWhiteSpace())
                    {
                        try
                        {
                            var gameLookup = GetGameByImdbId(parserResult.ImdbId);
                            return gameLookup == null ? new List<Game>() : new List<Game> { _gameService.FindByIgdbId(gameLookup.IgdbId) ?? new Game { GameMetadata = gameLookup } };
                        }
                        catch (Exception)
                        {
                            return new List<Game>();
                        }
                    }

                    if (parserResult.IgdbId > 0)
                    {
                        try
                        {
                            var gameLookup = GetGameInfo(parserResult.IgdbId).Item1;
                            return gameLookup == null ? new List<Game>() : new List<Game> { _gameService.FindByIgdbId(gameLookup.IgdbId) ?? new Game { GameMetadata = gameLookup } };
                        }
                        catch (Exception)
                        {
                            return new List<Game>();
                        }
                    }
                }

                parserTitle = StripTrailingTheFromTitle(parserTitle);

                if (lowerTitle.StartsWith("imdb:") || lowerTitle.StartsWith("imdbid:"))
                {
                    var slug = lowerTitle.Split(':')[1].Trim();

                    var imdbid = slug;

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace))
                    {
                        return new List<Game>();
                    }

                    try
                    {
                        var gameLookup = GetGameByImdbId(imdbid);
                        return gameLookup == null ? new List<Game>() : new List<Game> { _gameService.FindByIgdbId(gameLookup.IgdbId) ?? new Game { GameMetadata = gameLookup } };
                    }
                    catch (GameNotFoundException)
                    {
                        return new List<Game>();
                    }
                }

                if (lowerTitle.StartsWith("igdb:") || lowerTitle.StartsWith("igdbid:"))
                {
                    var slug = lowerTitle.Split(':')[1].Trim();

                    var igdbid = -1;

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace) || !int.TryParse(slug, out igdbid))
                    {
                        return new List<Game>();
                    }

                    try
                    {
                        var gameLookup = GetGameInfo(igdbid).Item1;
                        return gameLookup == null ? new List<Game>() : new List<Game> { _gameService.FindByIgdbId(gameLookup.IgdbId) ?? new Game { GameMetadata = gameLookup } };
                    }
                    catch (GameNotFoundException)
                    {
                        return new List<Game>();
                    }
                }

                var searchTerm = parserTitle.Replace("_", "+").Replace(" ", "+").Replace(".", "+");

                var firstChar = searchTerm.First();

                var request = _gamarrMetadata.Create()
                    .SetSegment("route", "search")
                    .AddQueryParam("q", searchTerm)
                    .AddQueryParam("year", yearTerm)
                    .Build();

                request.AllowAutoRedirect = true;
                request.SuppressHttpError = true;

                var httpResponse = _httpClient.Get<List<GameResource>>(request);

                return httpResponse.Resource.SelectList(MapSearchResult);
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex);
                throw new SkyHookException("Search for '{0}' failed. Unable to communicate with GamarrAPI. {1}", ex, title, ex.Message);
            }
            catch (WebException ex)
            {
                _logger.Warn(ex);
                throw new SkyHookException("Search for '{0}' failed. Unable to communicate with GamarrAPI. {1}", ex, title, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex);
                throw new SkyHookException("Search for '{0}' failed. Invalid response received from GamarrAPI. {1}", ex, title, ex.Message);
            }
        }

        private Game MapSearchResult(GameResource result)
        {
            var game = _gameService.FindByIgdbId(result.IgdbId);

            if (game == null)
            {
                game = new Game { GameMetadata = MapGame(result) };
            }
            else
            {
                game.GameMetadata.Value.Translations = _gameTranslationService.GetAllTranslationsForGameMetadata(game.GameMetadataId);
            }

            return game;
        }

        private GameCollection MapCollection(CollectionResource arg)
        {
            var collection = new GameCollection
            {
                IgdbId = arg.IgdbId,
                Title = arg.Name,
                Overview = arg.Overview,
                CleanTitle = arg.Name.CleanGameTitle(),
                SortTitle = Parser.Parser.NormalizeTitle(arg.Name),
                Images = arg.Images?.Select(MapImage).ToList() ?? new List<MediaCover.MediaCover>(),
                Games = arg.Parts?.Select(x => MapGame(x)).ToList() ?? new List<GameMetadata>()
            };

            return collection;
        }

        private static Credit MapCast(CastResource arg)
        {
            var newActor = new Credit
            {
                Name = arg.Name,
                Character = arg.Character,
                Order = arg.Order,
                CreditIgdbId = arg.CreditId,
                PersonIgdbId = arg.IgdbId,
                Type = CreditType.Cast,
                Images = arg.Images.Select(MapImage).ToList()
            };

            return newActor;
        }

        private static Credit MapCrew(CrewResource arg)
        {
            var newActor = new Credit
            {
                Name = arg.Name,
                Department = arg.Department,
                Job = arg.Job,
                Order = arg.Order,
                CreditIgdbId = arg.CreditId,
                PersonIgdbId = arg.IgdbId,
                Type = CreditType.Crew,
                Images = arg.Images.Select(MapImage).ToList()
            };

            return newActor;
        }

        private static AlternativeTitle MapAlternativeTitle(AlternativeTitleResource arg)
        {
            var newAlternativeTitle = new AlternativeTitle
            {
                Title = arg.Title,
                SourceType = SourceType.Igdb,
                CleanTitle = arg.Title.CleanGameTitle()
            };

            return newAlternativeTitle;
        }

        private static GameTranslation MapTranslation(TranslationResource arg)
        {
            var newAlternativeTitle = new GameTranslation
            {
                Title = arg.Title,
                Overview = arg.Overview,
                CleanTitle = arg.Title.CleanGameTitle(),
                Language = IsoLanguages.Find(arg.Language.ToLower())?.Language
            };

            return newAlternativeTitle;
        }

        private static Ratings MapRatings(RatingResource ratings)
        {
            if (ratings == null)
            {
                return new Ratings();
            }

            var mappedRatings = new Ratings();

            if (ratings.Igdb != null)
            {
                mappedRatings.Igdb = new RatingChild
                {
                    Type = (RatingType)Enum.Parse(typeof(RatingType), ratings.Igdb.Type),
                    Value = ratings.Igdb.Value,
                    Votes = ratings.Igdb.Count
                };
            }

            if (ratings.Imdb != null)
            {
                mappedRatings.Imdb = new RatingChild
                {
                    Type = (RatingType)Enum.Parse(typeof(RatingType), ratings.Imdb.Type),
                    Value = ratings.Imdb.Value,
                    Votes = ratings.Imdb.Count
                };
            }

            if (ratings.Metacritic != null)
            {
                mappedRatings.Metacritic = new RatingChild
                {
                    Type = (RatingType)Enum.Parse(typeof(RatingType), ratings.Metacritic.Type),
                    Value = ratings.Metacritic.Value,
                    Votes = ratings.Metacritic.Count
                };
            }

            if (ratings.RottenTomatoes != null)
            {
                mappedRatings.RottenTomatoes = new RatingChild
                {
                    Type = (RatingType)Enum.Parse(typeof(RatingType), ratings.RottenTomatoes.Type),
                    Value = ratings.RottenTomatoes.Value,
                    Votes = ratings.RottenTomatoes.Count
                };
            }

            if (ratings.Trakt != null)
            {
                mappedRatings.Trakt = new RatingChild
                {
                    Type = (RatingType)Enum.Parse(typeof(RatingType), ratings.Trakt.Type),
                    Value = ratings.Trakt.Value,
                    Votes = ratings.Trakt.Count
                };
            }

            return mappedRatings;
        }

        private static MediaCover.MediaCover MapImage(ImageResource arg)
        {
            return new MediaCover.MediaCover
            {
                RemoteUrl = arg.Url,
                CoverType = MapCoverType(arg.CoverType)
            };
        }

        private static MediaCoverTypes MapCoverType(string coverType)
        {
            switch (coverType.ToLower())
            {
                case "poster":
                    return MediaCoverTypes.Poster;
                case "headshot":
                    return MediaCoverTypes.Headshot;
                case "fanart":
                    return MediaCoverTypes.Fanart;
                case "clearlogo":
                    return MediaCoverTypes.Clearlogo;
                default:
                    return MediaCoverTypes.Unknown;
            }
        }
    }
}
