using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.IGDB.Resource
{
    /// <summary>
    /// IGDB Game API Response
    /// https://api-docs.igdb.com/#game
    /// </summary>
    public class IgdbGameResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("storyline")]
        public string Storyline { get; set; }

        [JsonProperty("category")]
        public int Category { get; set; }

        [JsonProperty("status")]
        public int? Status { get; set; }

        [JsonProperty("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        [JsonProperty("aggregated_rating")]
        public double? AggregatedRating { get; set; }

        [JsonProperty("aggregated_rating_count")]
        public int? AggregatedRatingCount { get; set; }

        [JsonProperty("rating")]
        public double? Rating { get; set; }

        [JsonProperty("rating_count")]
        public int? RatingCount { get; set; }

        [JsonProperty("total_rating")]
        public double? TotalRating { get; set; }

        [JsonProperty("total_rating_count")]
        public int? TotalRatingCount { get; set; }

        [JsonProperty("hypes")]
        public int? Hypes { get; set; }

        [JsonProperty("follows")]
        public int? Follows { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        // Expanded fields (require field expansion in query)
        [JsonProperty("cover")]
        public IgdbImageResource Cover { get; set; }

        [JsonProperty("screenshots")]
        public List<IgdbImageResource> Screenshots { get; set; }

        [JsonProperty("artworks")]
        public List<IgdbImageResource> Artworks { get; set; }

        [JsonProperty("videos")]
        public List<IgdbVideoResource> Videos { get; set; }

        [JsonProperty("genres")]
        public List<IgdbGenreResource> Genres { get; set; }

        [JsonProperty("themes")]
        public List<IgdbThemeResource> Themes { get; set; }

        [JsonProperty("game_modes")]
        public List<IgdbGameModeResource> GameModes { get; set; }

        [JsonProperty("platforms")]
        public List<IgdbPlatformResource> Platforms { get; set; }

        [JsonProperty("involved_companies")]
        public List<IgdbInvolvedCompanyResource> InvolvedCompanies { get; set; }

        [JsonProperty("game_engines")]
        public List<IgdbGameEngineResource> GameEngines { get; set; }

        [JsonProperty("keywords")]
        public List<IgdbKeywordResource> Keywords { get; set; }

        [JsonProperty("alternative_names")]
        public List<IgdbAlternativeNameResource> AlternativeNames { get; set; }

        [JsonProperty("collection")]
        public IgdbCollectionResource Collection { get; set; }

        [JsonProperty("franchise")]
        public IgdbFranchiseResource Franchise { get; set; }

        [JsonProperty("franchises")]
        public List<IgdbFranchiseResource> Franchises { get; set; }

        // Parent game for DLCs/expansions
        [JsonProperty("parent_game")]
        public IgdbGameResource ParentGame { get; set; }

        // DLCs and expansions
        [JsonProperty("dlcs")]
        public List<IgdbGameResource> Dlcs { get; set; }

        [JsonProperty("expansions")]
        public List<IgdbGameResource> Expansions { get; set; }

        [JsonProperty("standalone_expansions")]
        public List<IgdbGameResource> StandaloneExpansions { get; set; }

        [JsonProperty("remakes")]
        public List<IgdbGameResource> Remakes { get; set; }

        [JsonProperty("remasters")]
        public List<IgdbGameResource> Remasters { get; set; }

        // Similar games for recommendations
        [JsonProperty("similar_games")]
        public List<IgdbGameResource> SimilarGames { get; set; }

        [JsonProperty("age_ratings")]
        public List<IgdbAgeRatingResource> AgeRatings { get; set; }

        [JsonProperty("release_dates")]
        public List<IgdbReleaseDateResource> ReleaseDates { get; set; }

        [JsonProperty("websites")]
        public List<IgdbWebsiteResource> Websites { get; set; }

        [JsonProperty("external_games")]
        public List<IgdbExternalGameResource> ExternalGames { get; set; }
    }

    public class IgdbImageResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("image_id")]
        public string ImageId { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets the full URL for an image with specified size
        /// Sizes: cover_small, cover_big, screenshot_med, screenshot_big, screenshot_huge, thumb, micro, 720p, 1080p
        /// </summary>
        public string GetImageUrl(string size = "cover_big")
        {
            if (string.IsNullOrEmpty(ImageId))
            {
                return null;
            }

            return $"https://images.igdb.com/igdb/image/upload/t_{size}/{ImageId}.jpg";
        }
    }

    public class IgdbVideoResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("video_id")]
        public string VideoId { get; set; }
    }

    public class IgdbGenreResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }
    }

    public class IgdbThemeResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }
    }

    public class IgdbGameModeResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }
    }

    public class IgdbPlatformResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("abbreviation")]
        public string Abbreviation { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("category")]
        public int? Category { get; set; }

        [JsonProperty("generation")]
        public int? Generation { get; set; }

        [JsonProperty("platform_family")]
        public IgdbPlatformFamilyResource PlatformFamily { get; set; }

        [JsonProperty("platform_logo")]
        public IgdbImageResource PlatformLogo { get; set; }
    }

    public class IgdbPlatformFamilyResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }
    }

    public class IgdbInvolvedCompanyResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("company")]
        public IgdbCompanyResource Company { get; set; }

        [JsonProperty("developer")]
        public bool Developer { get; set; }

        [JsonProperty("publisher")]
        public bool Publisher { get; set; }

        [JsonProperty("porting")]
        public bool Porting { get; set; }

        [JsonProperty("supporting")]
        public bool Supporting { get; set; }
    }

    public class IgdbCompanyResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("logo")]
        public IgdbImageResource Logo { get; set; }

        [JsonProperty("websites")]
        public List<IgdbWebsiteResource> Websites { get; set; }
    }

    public class IgdbGameEngineResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("logo")]
        public IgdbImageResource Logo { get; set; }
    }

    public class IgdbKeywordResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }
    }

    public class IgdbAlternativeNameResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }

    public class IgdbCollectionResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("games")]
        public List<IgdbGameResource> Games { get; set; }
    }

    public class IgdbFranchiseResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("games")]
        public List<IgdbGameResource> Games { get; set; }
    }

    public class IgdbAgeRatingResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category")]
        public int Category { get; set; }

        [JsonProperty("rating")]
        public int Rating { get; set; }

        [JsonProperty("synopsis")]
        public string Synopsis { get; set; }

        /// <summary>
        /// Age rating category:
        /// 1 = ESRB, 2 = PEGI, 3 = CERO, 4 = USK, 5 = GRAC, 6 = CLASS_IND, 7 = ACB
        /// </summary>
        public string GetRatingOrganization()
        {
            return Category switch
            {
                1 => "ESRB",
                2 => "PEGI",
                3 => "CERO",
                4 => "USK",
                5 => "GRAC",
                6 => "CLASS_IND",
                7 => "ACB",
                _ => "Unknown"
            };
        }
    }

    public class IgdbReleaseDateResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("date")]
        public long? Date { get; set; }

        [JsonProperty("human")]
        public string Human { get; set; }

        [JsonProperty("category")]
        public int Category { get; set; }

        [JsonProperty("region")]
        public int? Region { get; set; }

        [JsonProperty("platform")]
        public IgdbPlatformResource Platform { get; set; }
    }

    public class IgdbWebsiteResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category")]
        public int Category { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("trusted")]
        public bool? Trusted { get; set; }

        /// <summary>
        /// Website category:
        /// 1=official, 2=wikia, 3=wikipedia, 4=facebook, 5=twitter, 6=twitch,
        /// 8=instagram, 9=youtube, 10=iphone, 11=ipad, 12=android, 13=steam,
        /// 14=reddit, 15=itch, 16=epicgames, 17=gog, 18=discord
        /// </summary>
        public string GetWebsiteType()
        {
            return Category switch
            {
                1 => "official",
                2 => "wikia",
                3 => "wikipedia",
                4 => "facebook",
                5 => "twitter",
                6 => "twitch",
                8 => "instagram",
                9 => "youtube",
                10 => "iphone",
                11 => "ipad",
                12 => "android",
                13 => "steam",
                14 => "reddit",
                15 => "itch",
                16 => "epicgames",
                17 => "gog",
                18 => "discord",
                _ => "unknown"
            };
        }
    }

    public class IgdbExternalGameResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category")]
        public int Category { get; set; }

        [JsonProperty("game")]
        public int? Game { get; set; }

        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// External game category:
        /// 1=steam, 5=gog, 10=youtube, 11=microsoft, 13=apple, 14=twitch,
        /// 15=android, 20=amazon_game, 22=amazon_luna, 23=amazon_asin,
        /// 26=epicgames, 28=oculus, 29=utomik, 30=itch, 31=xbox, 32=ps5, etc.
        /// </summary>
        public string GetPlatformName()
        {
            return Category switch
            {
                1 => "Steam",
                5 => "GOG",
                10 => "YouTube",
                11 => "Microsoft",
                13 => "Apple",
                14 => "Twitch",
                15 => "Android",
                20 => "Amazon Games",
                22 => "Amazon Luna",
                26 => "Epic Games",
                28 => "Oculus",
                29 => "Utomik",
                30 => "itch.io",
                31 => "Xbox Marketplace",
                32 => "PlayStation Store",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets Steam App ID if this is a Steam external game
        /// </summary>
        public int? GetSteamAppId()
        {
            if (Category == 1 && !string.IsNullOrEmpty(Uid) && int.TryParse(Uid, out var steamId))
            {
                return steamId;
            }

            return null;
        }
    }
}
