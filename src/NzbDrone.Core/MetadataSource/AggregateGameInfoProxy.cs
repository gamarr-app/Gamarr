using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Credits;
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

        public Tuple<GameMetadata, List<Credit>> GetGameInfo(int gameId)
        {
            // Try Steam first - the ID might be a Steam App ID
            try
            {
                var result = _steamProxy.GetGameInfo(gameId);
                if (result?.Item1 != null)
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
                    if (result?.Item1 != null)
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
                    if (result?.Item1 != null)
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
            return new Tuple<GameMetadata, List<Credit>>(null, new List<Credit>());
        }

        /// <summary>
        /// Get game info by Steam App ID (no API key required).
        /// </summary>
        public Tuple<GameMetadata, List<Credit>> GetGameInfoBySteamAppId(int steamAppId)
        {
            try
            {
                var result = _steamProxy.GetGameInfo(steamAppId);
                if (result?.Item1 != null)
                {
                    _logger.Debug("Got game info from Steam for App ID {0}", steamAppId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to get game info from Steam for App ID {0}", steamAppId);
            }

            return new Tuple<GameMetadata, List<Credit>>(null, new List<Credit>());
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
            var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Try IGDB first (preferred secondary source)
            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.GetTrendingGames();
                    foreach (var game in igdbResults)
                    {
                        if (seenTitles.Add(game.Title))
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
                        if (seenTitles.Add(game.Title))
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
            var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Try IGDB first (preferred secondary source)
            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.GetPopularGames();
                    foreach (var game in igdbResults)
                    {
                        if (seenTitles.Add(game.Title))
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
                        if (seenTitles.Add(game.Title))
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
            var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                    if (seenTitles.Add(game.Title))
                    {
                        allResults.Add(game);
                    }
                }

                _logger.Debug("Found {0} games from Steam for '{1}'", steamResults.Count, title);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to search Steam for '{0}'", title);
            }

            // Search IGDB next (preferred secondary source)
            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.SearchForNewGame(title);
                    foreach (var game in igdbResults)
                    {
                        if (seenTitles.Add(game.Title))
                        {
                            allResults.Add(game);
                        }
                    }

                    _logger.Debug("Found {0} additional games from IGDB for '{1}'", igdbResults.Count, title);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to search IGDB for '{0}'", title);
                }
            }

            // Also search RAWG for additional coverage
            if (HasRawgCredentials)
            {
                try
                {
                    var rawgResults = _rawgProxy.SearchForNewGame(title);
                    foreach (var game in rawgResults)
                    {
                        if (seenTitles.Add(game.Title))
                        {
                            allResults.Add(game);
                        }
                    }

                    _logger.Debug("Found {0} additional games from RAWG for '{1}'", rawgResults.Count, title);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to search RAWG for '{0}'", title);
                }
            }

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
    }
}
