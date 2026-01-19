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
            // Try RAWG first if configured
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
                    _logger.Warn(ex, "Failed to get game info from RAWG for ID {0}, trying IGDB", gameId);
                }
            }

            // Fall back to IGDB
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
                    _logger.Warn(ex, "Failed to get game info from IGDB for ID {0}", gameId);
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

            // Try RAWG first
            if (HasRawgCredentials)
            {
                try
                {
                    results = _rawgProxy.GetBulkGameInfo(gameIds);
                    if (results.Any())
                    {
                        return results;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get bulk game info from RAWG, trying IGDB");
                }
            }

            // Fall back to IGDB
            if (HasIgdbCredentials)
            {
                try
                {
                    results = _igdbProxy.GetBulkGameInfo(gameIds);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get bulk game info from IGDB");
                }
            }

            return results;
        }

        public List<GameMetadata> GetTrendingGames()
        {
            var allResults = new List<GameMetadata>();

            // Get from both sources and merge
            if (HasRawgCredentials)
            {
                try
                {
                    allResults.AddRange(_rawgProxy.GetTrendingGames());
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get trending games from RAWG");
                }
            }

            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.GetTrendingGames();

                    // Add IGDB results that aren't already in the list (by title match)
                    foreach (var game in igdbResults)
                    {
                        if (!allResults.Any(r => r.Title.Equals(game.Title, StringComparison.OrdinalIgnoreCase)))
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

            return allResults.Take(20).ToList();
        }

        public List<GameMetadata> GetPopularGames()
        {
            var allResults = new List<GameMetadata>();

            // Get from both sources and merge
            if (HasRawgCredentials)
            {
                try
                {
                    allResults.AddRange(_rawgProxy.GetPopularGames());
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get popular games from RAWG");
                }
            }

            if (HasIgdbCredentials)
            {
                try
                {
                    var igdbResults = _igdbProxy.GetPopularGames();
                    foreach (var game in igdbResults)
                    {
                        if (!allResults.Any(r => r.Title.Equals(game.Title, StringComparison.OrdinalIgnoreCase)))
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

            // Search RAWG if configured
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

            // Also search IGDB to get additional results
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

            if (!allResults.Any())
            {
                _logger.Warn("No results found for '{0}' from any metadata source", title);
            }

            return allResults;
        }

        public GameMetadata MapGameToIgdbGame(GameMetadata game)
        {
            // Try RAWG first
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

            // Fall back to IGDB
            if (HasIgdbCredentials)
            {
                try
                {
                    return _igdbProxy.MapGameToIgdbGame(game);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to map game via IGDB");
                }
            }

            return game;
        }
    }
}
