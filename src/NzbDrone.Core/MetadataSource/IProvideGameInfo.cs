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

        // Bulk lookups by specific ID type
        List<GameMetadata> GetBulkGameInfoByIgdbIds(List<int> igdbIds);
        List<GameMetadata> GetBulkGameInfoBySteamAppIds(List<int> steamAppIds);
        List<GameMetadata> GetBulkGameInfoByRawgIds(List<int> rawgIds);

        // Collection info
        GameCollection GetCollectionInfo(int igdbId);

        // Discovery
        List<GameMetadata> GetTrendingGames();
        List<GameMetadata> GetPopularGames();

        // Similar games / Recommendations
        List<int> GetSimilarGames(string title, int igdbId);

        // Changed games tracking
        HashSet<int> GetChangedGames(DateTime startTime);
    }
}
