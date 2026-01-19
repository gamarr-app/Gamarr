using System;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;

#pragma warning disable CS0618 // Disable obsolete warnings for ImdbId (kept for backward compatibility)

namespace NzbDrone.Core.ImportLists.ImportListGames
{
    public class ImportListGame : ModelBase
    {
        public ImportListGame()
        {
            GameMetadata = new GameMetadata();
        }

        public int ListId { get; set; }
        public int GameMetadataId { get; set; }
        public LazyLoaded<GameMetadata> GameMetadata { get; set; }

        public string Title
        {
            get { return GameMetadata.Value.Title; }
            set { GameMetadata.Value.Title = value; }
        }

        /// <summary>
        /// Primary identifier - Steam App ID
        /// </summary>
        public int SteamAppId
        {
            get { return GameMetadata.Value.SteamAppId; }
            set { GameMetadata.Value.SteamAppId = value; }
        }

        /// <summary>
        /// Secondary identifier - IGDB ID (for metadata enrichment)
        /// </summary>
        public int IgdbId
        {
            get { return GameMetadata.Value.IgdbId; }
            set { GameMetadata.Value.IgdbId = value; }
        }

        /// <summary>
        /// Secondary identifier - RAWG ID (for metadata enrichment)
        /// </summary>
        public int RawgId
        {
            get { return GameMetadata.Value.RawgId; }
            set { GameMetadata.Value.RawgId = value; }
        }

        /// <summary>
        /// DEPRECATED: IMDb is a movie database and does not apply to games.
        /// This property is kept for backwards compatibility but will always be null/empty for games.
        /// </summary>
        [Obsolete("IMDb is a movie database and does not apply to games. This property will always be null/empty.")]
        public string ImdbId
        {
            get { return GameMetadata.Value.ImdbId; }
            set { GameMetadata.Value.ImdbId = value; }
        }

        public int Year
        {
            get { return GameMetadata.Value.Year; }
            set { GameMetadata.Value.Year = value; }
        }
    }
}
