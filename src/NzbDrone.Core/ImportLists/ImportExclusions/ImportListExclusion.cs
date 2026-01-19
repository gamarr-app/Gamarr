using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.ImportLists.ImportExclusions
{
    public class ImportListExclusion : ModelBase
    {
        /// <summary>
        /// Primary identifier - Steam App ID
        /// </summary>
        public int SteamAppId { get; set; }

        /// <summary>
        /// Secondary identifier - IGDB ID (for metadata enrichment)
        /// </summary>
        public int IgdbId { get; set; }

        public string GameTitle { get; set; }
        public int GameYear { get; set; }

        public new string ToString()
        {
            return string.Format("Excluded Game: [Steam:{0}][IGDB:{1}][{2} {3}]", SteamAppId, IgdbId, GameTitle, GameYear);
        }
    }
}
