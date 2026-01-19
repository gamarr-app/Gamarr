using System;
using System.Collections.Generic;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Credits;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideGameInfo
    {
        // Primary identifier - Steam App ID
        GameMetadata GetGameBySteamAppId(int steamAppId);
        Tuple<GameMetadata, List<Credit>> GetGameInfoBySteamAppId(int steamAppId);

        // Secondary identifiers
        Tuple<GameMetadata, List<Credit>> GetGameInfo(int igdbId);
        List<GameMetadata> GetBulkGameInfo(List<int> igdbIds);

        // Collection info
        GameCollection GetCollectionInfo(int igdbId);

        // Discovery
        List<GameMetadata> GetTrendingGames();
        List<GameMetadata> GetPopularGames();

        // Changed games tracking
        HashSet<int> GetChangedGames(DateTime startTime);

        // Deprecated
        [Obsolete("IMDb lookup is not applicable to games")]
        GameMetadata GetGameByImdbId(string imdbId);
    }
}
