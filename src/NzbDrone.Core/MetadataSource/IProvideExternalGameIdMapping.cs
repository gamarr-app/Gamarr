using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource
{
    /// <summary>
    /// Maps store-specific product ids to IGDB game ids via the IGDB external_games endpoint.
    /// Implemented by IgdbProxy; only useful when IGDB credentials are configured.
    /// </summary>
    public interface IProvideExternalGameIdMapping
    {
        /// <summary>
        /// Batch-resolves GOG product ids to IGDB game ids
        /// (IGDB external_games category 5 = GOG).
        /// </summary>
        /// <returns>Dictionary of GOG product id to IGDB game id. Unmatched ids are absent.</returns>
        Dictionary<long, int> GetIgdbIdsByGogIds(ICollection<long> gogIds);
    }
}
