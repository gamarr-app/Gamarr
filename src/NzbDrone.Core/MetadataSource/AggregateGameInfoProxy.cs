using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.MetadataSource.IGDB;
using NzbDrone.Core.MetadataSource.RAWG;
using NzbDrone.Core.MetadataSource.Steam;

namespace NzbDrone.Core.MetadataSource
{
    /// <summary>
    /// Aggregates game metadata from multiple sources (Steam, RAWG, and IGDB).
    /// Steam works out of the box (no API key required!).
    /// RAWG and IGDB provide additional coverage with API keys.
    /// </summary>
    public class AggregateGameInfoProxy : IProvideGameInfo, ISearchForNewGame
    {
        private readonly SteamStoreProxy _steamProxy;
        private readonly RawgProxy _rawgProxy;
        private readonly IgdbProxy _igdbProxy;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public AggregateGameInfoProxy(
            SteamStoreProxy steamProxy,
            RawgProxy rawgProxy,
            IgdbProxy igdbProxy,
            IConfigService configService,
            Logger logger)
        {
            _steamProxy = steamProxy;
            _rawgProxy = rawgProxy;
            _igdbProxy = igdbProxy;
            _configService = configService;
            _logger = logger;
        }

        // Steam works out of the box - no credentials needed!
        private bool HasRawgCredentials => !string.IsNullOrEmpty(_configService.RawgApiKey);
        private bool HasIgdbCredentials => !string.IsNullOrEmpty(_configService.IgdbClientId) &&
                                           !string.IsNullOrEmpty(_configService.IgdbClientSecret);

        public GameMetadata GetGameInfo(int gameId)
        {
            // Try Steam first - the ID might be a Steam App ID
            try
            {
                var result = _steamProxy.GetGameInfo(gameId);
                if (result != null)
                {
                    _logger.Debug("Got game info from Steam for ID {0}", gameId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "ID {0} not found in Steam, trying IGDB", gameId);
            }

            // Try IGDB (preferred secondary source)
            if (HasIgdbCredentials)
            {
                try
                {
                    var result = _igdbProxy.GetGameInfo(gameId);
                    if (result != null)
                    {
                        _logger.Debug("Got game info from IGDB for ID {0}", gameId);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get game info from IGDB for ID {0}, trying RAWG", gameId);
                }
            }

            // Fall back to RAWG
            if (HasRawgCredentials)
            {
                try
                {
                    var result = _rawgProxy.GetGameInfo(gameId);
                    if (result != null)
                    {
                        _logger.Debug("Got game info from RAWG for ID {0}", gameId);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get game info from RAWG for ID {0}", gameId);
                }
            }

            _logger.Warn("No metadata source available or all sources failed for game ID {0}", gameId);
            return null;
        }

        /// <summary>
        /// Get game info by Steam App ID (no API key required).
        /// </summary>
        public GameMetadata GetGameInfoBySteamAppId(int steamAppId)
        {
            try
            {
                var result = _steamProxy.GetGameInfo(steamAppId);
                if (result != null)
                {
                    _logger.Debug("Got game info from Steam for App ID {0}", steamAppId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to get game info from Steam for App ID {0}", steamAppId);
            }

            return null;
        }

        /// <summary>
        /// Get game by Steam App ID - primary identifier, no API key required.
        /// </summary>
        public GameMetadata GetGameBySteamAppId(int steamAppId)
        {
            // Try Steam first (no API key needed!)
            try
            {
                var result = _steamProxy.GetGameBySteamAppId(steamAppId);
                if (result != null)
                {
                    _logger.Debug("Got game info from Steam for App ID {0}", steamAppId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to get game from Steam for App ID {0}", steamAppId);
            }

            // Try IGDB to cross-reference
            if (HasIgdbCredentials)
            {
                try
                {
                    var result = _igdbProxy.GetGameBySteamAppId(steamAppId);
                    if (result != null)
                    {
                        _logger.Debug("Got game info from IGDB for Steam App ID {0}", steamAppId);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get game from IGDB for Steam App ID {0}", steamAppId);
                }
            }

            // Try RAWG as last resort
            if (HasRawgCredentials)
            {
                try
                {
                    var result = _rawgProxy.GetGameBySteamAppId(steamAppId);
                    if (result != null)
                    {
                        _logger.Debug("Got game info from RAWG for Steam App ID {0}", steamAppId);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get game from RAWG for Steam App ID {0}", steamAppId);
                }
            }

            return null;
        }

        public GameCollection GetCollectionInfo(int collectionId)
        {
            // RAWG doesn't have collection support, try IGDB
            if (HasIgdbCredentials)
            {
                try
                {
                    return _igdbProxy.GetCollectionInfo(collectionId);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get collection info from IGDB: {0}", collectionId);
                }
            }

            return null;
        }

        public List<GameMetadata> GetBulkGameInfo(List<int> gameIds)
        {
            var results = new List<GameMetadata>();
            var remainingIds = new List<int>(gameIds);

            // Try Steam first for each ID (no API key needed)
            // Limit to avoid too many API calls
            foreach (var id in gameIds.Take(20))
            {
                try
                {
                    var steamResult = _steamProxy.GetGameBySteamAppId(id);
                    if (steamResult != null)
                    {
                        results.Add(steamResult);
                        remainingIds.Remove(id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "ID {0} not found in Steam", id);
                }
            }

            if (!remainingIds.Any())
            {
                return results;
            }

            // Try IGDB for remaining IDs (preferred secondary source)
            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.GetBulkGameInfo(remainingIds);
                    results.AddRange(igdbResults);

                    // Remove found IDs
                    foreach (var game in igdbResults)
                    {
                        remainingIds.Remove(game.IgdbId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get bulk game info from IGDB, trying RAWG");
                }
            }

            if (!remainingIds.Any())
            {
                return results;
            }

            // Fall back to RAWG for any remaining
            if (HasRawgCredentials)
            {
                try
                {
                    var rawgResults = _rawgProxy.GetBulkGameInfo(remainingIds);
                    results.AddRange(rawgResults);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get bulk game info from RAWG");
                }
            }

            return results;
        }

        public List<GameMetadata> GetTrendingGames()
        {
            var allResults = new List<GameMetadata>();
            var seenNormalizedTitles = new HashSet<string>(StringComparer.Ordinal);

            // Try IGDB first (preferred secondary source)
            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.GetTrendingGames();
                    foreach (var game in igdbResults)
                    {
                        var normalizedTitle = NormalizeTitleForComparison(game.Title);
                        if (seenNormalizedTitles.Add(normalizedTitle))
                        {
                            allResults.Add(game);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get trending games from IGDB");
                }
            }

            // Add RAWG results that aren't already in the list
            if (HasRawgCredentials)
            {
                try
                {
                    var rawgResults = _rawgProxy.GetTrendingGames();
                    foreach (var game in rawgResults)
                    {
                        var normalizedTitle = NormalizeTitleForComparison(game.Title);
                        if (seenNormalizedTitles.Add(normalizedTitle))
                        {
                            allResults.Add(game);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get trending games from RAWG");
                }
            }

            return allResults.Take(20).ToList();
        }

        public List<GameMetadata> GetPopularGames()
        {
            var allResults = new List<GameMetadata>();
            var seenNormalizedTitles = new HashSet<string>(StringComparer.Ordinal);

            // Try IGDB first (preferred secondary source)
            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.GetPopularGames();
                    foreach (var game in igdbResults)
                    {
                        var normalizedTitle = NormalizeTitleForComparison(game.Title);
                        if (seenNormalizedTitles.Add(normalizedTitle))
                        {
                            allResults.Add(game);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get popular games from IGDB");
                }
            }

            // Add RAWG results that aren't already in the list
            if (HasRawgCredentials)
            {
                try
                {
                    var rawgResults = _rawgProxy.GetPopularGames();
                    foreach (var game in rawgResults)
                    {
                        var normalizedTitle = NormalizeTitleForComparison(game.Title);
                        if (seenNormalizedTitles.Add(normalizedTitle))
                        {
                            allResults.Add(game);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get popular games from RAWG");
                }
            }

            return allResults.Take(20).ToList();
        }

        public HashSet<int> GetChangedGames(DateTime startTime)
        {
            var changedIds = new HashSet<int>();

            // RAWG doesn't support change detection
            // Try IGDB
            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbChanges = _igdbProxy.GetChangedGames(startTime);
                    foreach (var id in igdbChanges)
                    {
                        changedIds.Add(id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get changed games from IGDB");
                }
            }

            return changedIds;
        }

        public List<Game> SearchForNewGame(string title)
        {
            var allResults = new List<Game>();
            var resultsByNormalizedTitle = new Dictionary<string, Game>(StringComparer.Ordinal);
            var lowerTitle = title?.ToLowerInvariant()?.Trim() ?? string.Empty;

            // Handle direct IGDB ID lookups - these bypass normal search and credentials check
            if (lowerTitle.StartsWith("igdb:") || lowerTitle.StartsWith("igdbid:"))
            {
                try
                {
                    var igdbResults = _igdbProxy.SearchForNewGame(title);
                    return igdbResults;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to lookup IGDB game for '{0}'", title);
                    return new List<Game>();
                }
            }

            // Search Steam first (no API key needed - works out of the box!)
            try
            {
                var steamResults = _steamProxy.SearchForNewGame(title);
                foreach (var game in steamResults)
                {
                    var normalizedTitle = NormalizeTitleForComparison(game.Title);
                    resultsByNormalizedTitle[normalizedTitle] = game;
                }

                _logger.Debug("Found {0} games from Steam for '{1}'", steamResults.Count, title);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to search Steam for '{0}'", title);
            }

            // Search IGDB next (preferred secondary source)
            // Merge IGDB metadata into existing results to get best of both sources
            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.SearchForNewGame(title);
                    foreach (var game in igdbResults)
                    {
                        var normalizedTitle = NormalizeTitleForComparison(game.Title);
                        if (resultsByNormalizedTitle.TryGetValue(normalizedTitle, out var existing))
                        {
                            // Merge IGDB data into existing result
                            MergeGameMetadata(existing, game);
                            _logger.Debug("Merged IGDB metadata into existing result for '{0}'", game.Title);
                        }
                        else
                        {
                            resultsByNormalizedTitle[normalizedTitle] = game;
                        }
                    }

                    _logger.Debug("Found {0} games from IGDB for '{1}'", igdbResults.Count, title);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to search IGDB for '{0}'", title);
                }
            }

            // Also search RAWG for additional coverage
            // Merge RAWG metadata into existing results
            if (HasRawgCredentials)
            {
                try
                {
                    var rawgResults = _rawgProxy.SearchForNewGame(title);
                    foreach (var game in rawgResults)
                    {
                        var normalizedTitle = NormalizeTitleForComparison(game.Title);
                        if (resultsByNormalizedTitle.TryGetValue(normalizedTitle, out var existing))
                        {
                            // Merge RAWG data into existing result
                            MergeGameMetadata(existing, game);
                            _logger.Debug("Merged RAWG metadata into existing result for '{0}'", game.Title);
                        }
                        else
                        {
                            resultsByNormalizedTitle[normalizedTitle] = game;
                        }
                    }

                    _logger.Debug("Found {0} games from RAWG for '{1}'", rawgResults.Count, title);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to search RAWG for '{0}'", title);
                }
            }

            allResults = resultsByNormalizedTitle.Values.ToList();

            if (!allResults.Any())
            {
                _logger.Warn("No results found for '{0}' from any metadata source", title);
            }

            return allResults;
        }

        public GameMetadata MapGameToIgdbGame(GameMetadata game)
        {
            // Try IGDB first (preferred secondary source)
            if (HasIgdbCredentials)
            {
                try
                {
                    var result = _igdbProxy.MapGameToIgdbGame(game);
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to map game via IGDB");
                }
            }

            // Fall back to RAWG
            if (HasRawgCredentials)
            {
                try
                {
                    var result = _rawgProxy.MapGameToIgdbGame(game);
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to map game via RAWG");
                }
            }

            return game;
        }

        /// <summary>
        /// Merges metadata from a secondary source into an existing game result.
        /// Takes the best data from each source (e.g., IGDB images, IDs, ratings).
        /// </summary>
        private void MergeGameMetadata(Game existing, Game secondary)
        {
            var existingMeta = existing.GameMetadata?.Value;
            var secondaryMeta = secondary.GameMetadata?.Value;

            if (existingMeta == null || secondaryMeta == null)
            {
                return;
            }

            // Add IGDB ID if missing
            if (existingMeta.IgdbId == 0 && secondaryMeta.IgdbId > 0)
            {
                existingMeta.IgdbId = secondaryMeta.IgdbId;
            }

            // Add IGDB slug if missing
            if (string.IsNullOrEmpty(existingMeta.IgdbSlug) && !string.IsNullOrEmpty(secondaryMeta.IgdbSlug))
            {
                existingMeta.IgdbSlug = secondaryMeta.IgdbSlug;
            }

            // Merge images - add IGDB images at the front (they're more reliable than Steam CDN)
            if (secondaryMeta.Images != null && secondaryMeta.Images.Any())
            {
                existingMeta.Images ??= new List<MediaCover.MediaCover>();

                // Insert IGDB images at the beginning so they're tried first
                var insertIndex = 0;
                foreach (var image in secondaryMeta.Images)
                {
                    // Only add if it's from IGDB (has igdb.com in URL)
                    if (image.RemoteUrl?.Contains("igdb.com") == true)
                    {
                        existingMeta.Images.Insert(insertIndex++, image);
                    }
                    else if (!existingMeta.Images.Any(i => i.CoverType == image.CoverType))
                    {
                        // For non-IGDB images, only add if we don't have this type
                        existingMeta.Images.Add(image);
                    }
                }
            }

            // Add IGDB rating if missing
            if (secondaryMeta.Ratings?.Igdb != null && existingMeta.Ratings?.Igdb == null)
            {
                existingMeta.Ratings ??= new Ratings();
                existingMeta.Ratings.Igdb = secondaryMeta.Ratings.Igdb;
            }

            // Add overview if missing
            if (string.IsNullOrEmpty(existingMeta.Overview) && !string.IsNullOrEmpty(secondaryMeta.Overview))
            {
                existingMeta.Overview = secondaryMeta.Overview;
            }

            // Add genres if missing
            if ((existingMeta.Genres == null || !existingMeta.Genres.Any()) &&
                secondaryMeta.Genres != null && secondaryMeta.Genres.Any())
            {
                existingMeta.Genres = secondaryMeta.Genres;
            }
        }

        /// <summary>
        /// Normalizes a title for comparison by removing punctuation, extra whitespace,
        /// and converting to lowercase. This helps identify duplicate games across providers
        /// that may format titles differently (e.g., "ELDEN RING NIGHTREIGN" vs "Elden Ring: Nightreign").
        /// </summary>
        private static string NormalizeTitleForComparison(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return string.Empty;
            }

            // Remove punctuation and special characters, keep only letters, numbers, and spaces
            var normalized = Regex.Replace(title, @"[^\p{L}\p{N}\s]", " ");

            // Normalize whitespace (multiple spaces to single space)
            normalized = Regex.Replace(normalized, @"\s+", " ");

            // Trim and lowercase
            return normalized.Trim().ToLowerInvariant();
        }
    }
}
