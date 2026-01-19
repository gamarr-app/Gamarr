using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.RAWG.Resource
{
    public class RawgSearchResult
    {
        public int Count { get; set; }
        public string Next { get; set; }
        public string Previous { get; set; }
        public List<RawgGameResource> Results { get; set; }
    }

    public class RawgGameResource
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Name_Original { get; set; }
        public string Description { get; set; }
        public string Description_Raw { get; set; }
        public string Released { get; set; }
        public bool Tba { get; set; }
        public string Background_Image { get; set; }
        public double? Rating { get; set; }
        public int? Rating_Top { get; set; }
        public int? Ratings_Count { get; set; }
        public int? Reviews_Count { get; set; }
        public int? Metacritic { get; set; }
        public int? Playtime { get; set; }
        public DateTime? Updated { get; set; }
        public List<RawgPlatformWrapper> Platforms { get; set; }
        public List<RawgPlatformWrapper> Parent_Platforms { get; set; }
        public List<RawgStoreWrapper> Stores { get; set; }
        public List<RawgDeveloper> Developers { get; set; }
        public List<RawgPublisher> Publishers { get; set; }
        public List<RawgGenre> Genres { get; set; }
        public List<RawgTag> Tags { get; set; }
        public RawgEsrbRating Esrb_Rating { get; set; }
        public List<RawgScreenshot> Short_Screenshots { get; set; }
        public string Website { get; set; }
        public string Reddit_Url { get; set; }
        public int? Reddit_Count { get; set; }
        public string Metacritic_Url { get; set; }
        public int? Suggestions_Count { get; set; }
        public int? Achievements_Count { get; set; }
        public int? Additions_Count { get; set; }
        public int? Game_Series_Count { get; set; }
        public int? Movies_Count { get; set; }
        public int? Screenshots_Count { get; set; }
    }

    public class RawgPlatformWrapper
    {
        public RawgPlatform Platform { get; set; }
        public string Released_At { get; set; }
        public RawgRequirements Requirements { get; set; }
    }

    public class RawgPlatform
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Image { get; set; }
        public string Image_Background { get; set; }
    }

    public class RawgRequirements
    {
        public string Minimum { get; set; }
        public string Recommended { get; set; }
    }

    public class RawgStoreWrapper
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public RawgStore Store { get; set; }
    }

    public class RawgStore
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Domain { get; set; }
    }

    public class RawgDeveloper
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Image_Background { get; set; }
    }

    public class RawgPublisher
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Image_Background { get; set; }
    }

    public class RawgGenre
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
    }

    public class RawgTag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Language { get; set; }
        public int Games_Count { get; set; }
    }

    public class RawgEsrbRating
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
    }

    public class RawgScreenshot
    {
        public int Id { get; set; }
        public string Image { get; set; }
    }
}
