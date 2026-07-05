using System;
using System.Collections.Generic;

namespace GamarrLibrary.Mapping
{
    /// <summary>
    /// Subset of Gamarr's /api/v3/game GameResource that the plugin consumes.
    /// Property names bind case-insensitively against Gamarr's camelCase JSON.
    /// This file is intentionally free of PlayniteSDK types so the mapping
    /// logic can be unit tested on any runtime.
    /// </summary>
    public class GamarrGameDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string SortTitle { get; set; }
        public int Year { get; set; }
        public string Overview { get; set; }
        public string Path { get; set; }

        public bool? HasFile { get; set; }
        public long? SizeOnDisk { get; set; }

        public int SteamAppId { get; set; }
        public int IgdbId { get; set; }
        public string IgdbSlug { get; set; }
        public int RawgId { get; set; }
        public string TitleSlug { get; set; }

        public string Developer { get; set; }
        public string Publisher { get; set; }
        public string Website { get; set; }
        public List<string> Genres { get; set; }
        public List<GamarrPlatformDto> Platforms { get; set; }

        public DateTime? ReleaseDate { get; set; }
        public DateTime? DigitalRelease { get; set; }
        public DateTime? PhysicalRelease { get; set; }
    }

    public class GamarrPlatformDto
    {
        public string Name { get; set; }
        public string Abbreviation { get; set; }
    }
}
