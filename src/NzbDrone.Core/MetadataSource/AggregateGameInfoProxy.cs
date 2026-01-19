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

namespace NzbDrone.Core.MetadataSource
{
    /// <summary>
    /// Aggregates game metadata from multiple sources (RAWG and IGDB).
    /// Uses RAWG as primary (simpler API key auth) and IGDB as fallback/enrichment.
    /// </summary>
    public class AggregateGameInfoProxy : IProvideGameInfo, ISearchForNewGame
    {
        private readonly RawgProxy _rawgProxy;
        private readonly IgdbProxy _igdbProxy;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public AggregateGameInfoProxy(
            RawgProxy rawgProxy,
            IgdbProxy igdbProxy,
            IConfigService configService,
            Logger logger)
        {
            _rawgProxy = rawgProxy;
            _igdbProxy = igdbProxy;
            _configService = configService;
            _logger = logger;
        }

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

        public GameMetadata GetGameByImdbId(string imdbId)
        {
            // RAWG doesn't support IMDb lookup, try IGDB
            if (HasIgdbCredentials)
            {
                try
                {
                    return _igdbProxy.GetGameByImdbId(imdbId);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get game by IMDb ID from IGDB: {0}", imdbId);
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

            // Search RAWG first (primary)
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

                    _logger.Debug("Found {0} games from RAWG for '{1}'", rawgResults.Count, title);
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
                _logger.Warn("No metadata sources available or all searches failed for '{0}'", title);
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
