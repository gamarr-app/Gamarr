using System;
using System.Collections.Generic;

namespace GamarrLibrary.Mapping
{
    /// <summary>
    /// PlayniteSDK-free intermediate representation of a game ready for
    /// Playnite import. Converted to Playnite.SDK.Models.GameMetadata by
    /// the plugin adapter.
    /// </summary>
    public class MappedGame
    {
        public string GameId { get; set; }
        public string Name { get; set; }
        public string SortingName { get; set; }
        public string Description { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string InstallDirectory { get; set; }
        public bool IsInstalled { get; set; }
        public string CoverUrl { get; set; }
        public string Developer { get; set; }
        public string Publisher { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Platforms { get; set; } = new List<string>();
        public List<MappedLink> Links { get; set; } = new List<MappedLink>();
    }

    public class MappedLink
    {
        public MappedLink(string name, string url)
        {
            Name = name;
            Url = url;
        }

        public string Name { get; }
        public string Url { get; }
    }
}
