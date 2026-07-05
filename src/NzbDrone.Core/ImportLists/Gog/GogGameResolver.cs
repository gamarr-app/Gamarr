using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.MetadataSource;

namespace NzbDrone.Core.ImportLists.Gog
{
    /// <summary>
    /// Maps GOG products to Gamarr games. GOG has its own product ids, which the
    /// rest of the pipeline doesn't understand (sync requires an IGDB id or a
    /// Steam App id), so we resolve them here:
    /// 1. If IGDB is configured, a single batched external_games lookup
    ///    (category 5 = GOG) resolves product ids directly to IGDB ids.
    /// 2. Anything left over falls back to a title(+year) match through the
    ///    aggregate search, which caches per title and applies each provider's
    ///    own throttle (notably Steam's 1 req/sec) - no unthrottled N+1 loops.
    /// Unresolved items are returned without ids and are filtered out by
    /// IsValidItem in the import list, never poisoning the list cache.
    /// </summary>
    public interface IGogGameResolver
    {
        IList<ImportListGame> ResolveGames(IList<GogProduct> products);
    }

    public class GogGameResolver : IGogGameResolver
    {
        private readonly IProvideExternalGameIdMapping _externalGameIdMapping;
        private readonly ISearchForNewGame _searchForNewGame;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public GogGameResolver(IProvideExternalGameIdMapping externalGameIdMapping,
                               ISearchForNewGame searchForNewGame,
                               IConfigService configService,
                               Logger logger)
        {
            _externalGameIdMapping = externalGameIdMapping;
            _searchForNewGame = searchForNewGame;
            _configService = configService;
            _logger = logger;
        }

        private bool HasIgdbCredentials => _configService.IgdbClientId.IsNotNullOrWhiteSpace() &&
                                           _configService.IgdbClientSecret.IsNotNullOrWhiteSpace();

        public IList<ImportListGame> ResolveGames(IList<GogProduct> products)
        {
            var result = new List<ImportListGame>();

            if (products == null || products.Count == 0)
            {
                return result;
            }

            var igdbIdsByGogId = new Dictionary<long, int>();

            if (HasIgdbCredentials)
            {
                try
                {
                    var gogIds = products.Select(p => p.GogId).Where(id => id > 0).Distinct().ToList();
                    igdbIdsByGogId = _externalGameIdMapping.GetIgdbIdsByGogIds(gogIds);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "IGDB external-id lookup for GOG products failed, falling back to title search");
                }
            }

            foreach (var product in products)
            {
                var item = new ImportListGame
                {
                    Title = product.Title,
                    Year = product.Year
                };

                if (igdbIdsByGogId.TryGetValue(product.GogId, out var igdbId))
                {
                    item.IgdbId = igdbId;
                }
                else if (product.Title.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        MatchByTitle(product, item);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug(ex, "Title search failed for GOG product '{0}' ({1})", product.Title, product.GogId);
                    }
                }

                // Unresolved items are kept (title only, no ids) so paging logic sees
                // the full page; IsValidItem filters them out of the final list.
                result.Add(item);
            }

            var resolved = result.Count(i => i.IgdbId > 0 || i.SteamAppId > 0);
            _logger.Debug("Resolved {0} of {1} GOG products to known games", resolved, products.Count);

            return result;
        }

        private void MatchByTitle(GogProduct product, ImportListGame item)
        {
            var candidates = _searchForNewGame.SearchForNewGame(product.Title);

            if (candidates == null || candidates.Count == 0)
            {
                return;
            }

            var normalizedTitle = NormalizeTitle(product.Title);

            var titleMatches = candidates
                .Where(c => (c.IgdbId > 0 || c.SteamAppId > 0) && NormalizeTitle(c.Title) == normalizedTitle)
                .ToList();

            if (titleMatches.Count == 0)
            {
                _logger.Debug("No confident title match for GOG product '{0}' ({1})", product.Title, product.GogId);
                return;
            }

            Game match;

            if (product.Year > 0)
            {
                match = titleMatches.FirstOrDefault(c => c.Year == product.Year) ??
                        titleMatches.FirstOrDefault(c => Math.Abs(c.Year - product.Year) <= 1) ??
                        titleMatches.First();
            }
            else
            {
                match = titleMatches.First();
            }

            item.IgdbId = match.IgdbId;
            item.SteamAppId = match.SteamAppId;

            if (item.Year == 0)
            {
                item.Year = match.Year;
            }
        }

        private static string NormalizeTitle(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            return new string(title.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        }
    }
}
