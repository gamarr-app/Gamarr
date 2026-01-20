using System;
using System.Collections.Generic;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideGameInfo
    {
        // Primary identifier - Steam App ID
        GameMetadata GetGameBySteamAppId(int steamAppId);
        GameMetadata GetGameInfoBySteamAppId(int steamAppId);

        // Secondary identifiers
        GameMetadata GetGameInfo(int igdbId);
        List<GameMetadata> GetBulkGameInfo(List<int> igdbIds);

        // Collection info
        GameCollection GetCollectionInfo(int igdbId);

        // Discovery
        List<GameMetadata> GetTrendingGames();
        List<GameMetadata> GetPopularGames();

        // Changed games tracking
        HashSet<int> GetChangedGames(DateTime startTime);
    }
}
