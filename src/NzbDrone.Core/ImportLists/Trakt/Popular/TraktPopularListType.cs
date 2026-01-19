using System;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.Trakt.Popular
{
    public enum TraktPopularListType
    {
        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeTrendingGames")]
        Trending = 0,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypePopularGames")]
        Popular = 1,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeTopAnticipatedGames")]
        Anticipated = 2,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeTopBoxOfficeGames")]
        BoxOffice = 3,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeTopWatchedGamesByWeek")]
        TopWatchedByWeek = 4,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeTopWatchedGamesByMonth")]
        TopWatchedByMonth = 5,

        [Obsolete]
        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeTopWatchedGamesByYear")]
        TopWatchedByYear = 6,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeTopWatchedGamesOfAllTime")]
        TopWatchedByAllTime = 7,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeRecommendedGamesByWeek")]
        RecommendedByWeek = 8,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeRecommendedGamesByMonth")]
        RecommendedByMonth = 9,

        [Obsolete]
        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeRecommendedGamesByYear")]
        RecommendedByYear = 10,

        [FieldOption(Label = "ImportListsTraktSettingsPopularListTypeRecommendedGamesOfAllTime")]
        RecommendedByAllTime = 11
    }
}
