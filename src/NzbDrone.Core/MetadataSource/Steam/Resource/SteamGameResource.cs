using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NzbDrone.Core.MetadataSource.Steam.Resource
{
    /// <summary>
    /// Handles Steam's inconsistent API where pc_requirements can be either an object or an empty array [].
    /// </summary>
    public class SteamRequirementsConverter : JsonConverter<SteamPcRequirements>
    {
        public override SteamPcRequirements ReadJson(JsonReader reader, Type objectType, SteamPcRequirements existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            // Steam returns [] when there are no requirements
            if (token.Type == JTokenType.Array)
            {
                return null;
            }

            if (token.Type == JTokenType.Object)
            {
                return token.ToObject<SteamPcRequirements>();
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, SteamPcRequirements value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public class SteamAppDetailsResponse
    {
        public bool Success { get; set; }
        public SteamGameData Data { get; set; }
    }

    public class SteamGameData
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public int Steam_Appid { get; set; }
        public int Required_Age { get; set; }
        public bool Is_Free { get; set; }
        public string Detailed_Description { get; set; }
        public string About_The_Game { get; set; }
        public string Short_Description { get; set; }
        public string Supported_Languages { get; set; }
        public string Header_Image { get; set; }
        public string Capsule_Image { get; set; }
        public string Capsule_Imagev5 { get; set; }
        public string Website { get; set; }
        [JsonConverter(typeof(SteamRequirementsConverter))]
        public SteamPcRequirements Pc_Requirements { get; set; }

        [JsonConverter(typeof(SteamRequirementsConverter))]
        public SteamPcRequirements Mac_Requirements { get; set; }

        [JsonConverter(typeof(SteamRequirementsConverter))]
        public SteamPcRequirements Linux_Requirements { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
        public SteamPriceOverview Price_Overview { get; set; }
        public List<int> Packages { get; set; }
        public SteamPlatforms Platforms { get; set; }
        public SteamMetacritic Metacritic { get; set; }
        public List<SteamCategory> Categories { get; set; }
        public List<SteamGenre> Genres { get; set; }
        public List<SteamScreenshot> Screenshots { get; set; }
        public List<SteamMovie> Movies { get; set; }
        public SteamRecommendations Recommendations { get; set; }
        public SteamReleaseDate Release_Date { get; set; }
        public SteamSupportInfo Support_Info { get; set; }
        public string Background { get; set; }
        public string Background_Raw { get; set; }
        public SteamContentDescriptors Content_Descriptors { get; set; }

        // DLC parent game info
        public SteamFullGame Fullgame { get; set; }

        // DLC list (for base games)
        public List<int> Dlc { get; set; }
    }

    public class SteamFullGame
    {
        public string Appid { get; set; }
        public string Name { get; set; }
    }

    public class SteamPcRequirements
    {
        public string Minimum { get; set; }
        public string Recommended { get; set; }
    }

    public class SteamPriceOverview
    {
        public string Currency { get; set; }
        public int Initial { get; set; }
        public int Final { get; set; }
        public int Discount_Percent { get; set; }
        public string Initial_Formatted { get; set; }
        public string Final_Formatted { get; set; }
    }

    public class SteamPlatforms
    {
        public bool Windows { get; set; }
        public bool Mac { get; set; }
        public bool Linux { get; set; }
    }

    public class SteamMetacritic
    {
        public int Score { get; set; }
        public string Url { get; set; }
    }

    public class SteamCategory
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    public class SteamGenre
    {
        public string Id { get; set; }
        public string Description { get; set; }
    }

    public class SteamScreenshot
    {
        public int Id { get; set; }
        public string Path_Thumbnail { get; set; }
        public string Path_Full { get; set; }
    }

    public class SteamMovie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Thumbnail { get; set; }
        public SteamMovieFormats Webm { get; set; }
        public SteamMovieFormats Mp4 { get; set; }
        public bool Highlight { get; set; }
    }

    public class SteamMovieFormats
    {
        public string _480 { get; set; }
        public string Max { get; set; }
    }

    public class SteamRecommendations
    {
        public int Total { get; set; }
    }

    public class SteamReleaseDate
    {
        public bool Coming_Soon { get; set; }
        public string Date { get; set; }
    }

    public class SteamSupportInfo
    {
        public string Url { get; set; }
        public string Email { get; set; }
    }

    public class SteamContentDescriptors
    {
        public List<int> Ids { get; set; }
        public string Notes { get; set; }
    }

    // Search response models
    public class SteamSearchResponse
    {
        public int Total { get; set; }
        public List<SteamSearchItem> Items { get; set; }
    }

    public class SteamSearchItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("controllersupport")]
        public bool ControllerSupport { get; set; }

        [JsonProperty("price")]
        public SteamSearchPrice Price { get; set; }

        [JsonProperty("tiny_image")]
        public string TinyImage { get; set; }

        [JsonProperty("small_capsule_image")]
        public string SmallCapsuleImage { get; set; }

        [JsonProperty("large_capsule_image")]
        public string LargeCapsuleImage { get; set; }

        [JsonProperty("metascore")]
        public string Metascore { get; set; }

        [JsonProperty("platforms")]
        public SteamPlatforms Platforms { get; set; }
    }

    public class SteamSearchPrice
    {
        public string Currency { get; set; }
        public int Initial { get; set; }
        public int Final { get; set; }
    }

    // App list models
    public class SteamAppListResponse
    {
        public SteamAppList Applist { get; set; }
    }

    public class SteamAppList
    {
        public List<SteamApp> Apps { get; set; }
    }

    public class SteamApp
    {
        public int Appid { get; set; }
        public string Name { get; set; }
    }
}
