using System;
using System.Collections.Generic;
using System.Linq;
using Equ;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Translations;

namespace NzbDrone.Core.Games
{
    public class GameMetadata : Entity<GameMetadata>
    {
        public GameMetadata()
        {
            AlternativeTitles = new List<AlternativeTitle>();
            Translations = new List<GameTranslation>();
            Images = new List<MediaCover.MediaCover>();
            Genres = new List<string>();
            Keywords = new List<string>();
            OriginalLanguage = Language.English;
            Recommendations = new List<int>();
            Ratings = new Ratings();
            Platforms = new List<GamePlatform>();
            GameModes = new List<string>();
            Themes = new List<string>();
            DlcIds = new List<int>();
            GameType = GameType.MainGame;
        }

        public int IgdbId { get; set; }

        public List<MediaCover.MediaCover> Images { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Keywords { get; set; }
        public DateTime? InDevelopment { get; set; }
        public DateTime? PhysicalRelease { get; set; }
        public DateTime? DigitalRelease { get; set; }
        public string Certification { get; set; }
        public int Year { get; set; }
        public Ratings Ratings { get; set; }

        public int CollectionIgdbId { get; set; }
        public string CollectionTitle { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public int Runtime { get; set; }
        public string Website { get; set; }
        public string ImdbId { get; set; }
        public string Title { get; set; }
        public string CleanTitle { get; set; }
        public string SortTitle { get; set; }
        public GameStatusType Status { get; set; }
        public string Overview { get; set; }

        // Get Loaded via a Join Query
        public List<AlternativeTitle> AlternativeTitles { get; set; }
        public List<GameTranslation> Translations { get; set; }

        public int? SecondaryYear { get; set; }
        public string YouTubeTrailerId { get; set; }
        public string Studio { get; set; }
        public string OriginalTitle { get; set; }
        public string CleanOriginalTitle { get; set; }
        public Language OriginalLanguage { get; set; }
        public List<int> Recommendations { get; set; }
        public float Popularity { get; set; }

        /// <summary>
        /// Type of game content (Main game, DLC, Expansion, etc.)
        /// </summary>
        public GameType GameType { get; set; }

        /// <summary>
        /// IGDB ID of the parent game (for DLCs/expansions)
        /// </summary>
        public int? ParentGameId { get; set; }

        /// <summary>
        /// List of IGDB IDs for DLCs/expansions of this game
        /// </summary>
        public List<int> DlcIds { get; set; }

        /// <summary>
        /// Platforms this game is available on
        /// </summary>
        public List<GamePlatform> Platforms { get; set; }

        /// <summary>
        /// Game modes (Single player, Multiplayer, Co-op, etc.)
        /// </summary>
        public List<string> GameModes { get; set; }

        /// <summary>
        /// Game themes (Fantasy, Sci-fi, Horror, etc.)
        /// </summary>
        public List<string> Themes { get; set; }

        /// <summary>
        /// Developer companies
        /// </summary>
        public string Developer { get; set; }

        /// <summary>
        /// Publisher companies
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// Game engine used
        /// </summary>
        public string GameEngine { get; set; }

        /// <summary>
        /// IGDB aggregated rating (0-100)
        /// </summary>
        public double? AggregatedRating { get; set; }

        /// <summary>
        /// Number of ratings used for aggregated rating
        /// </summary>
        public int? AggregatedRatingCount { get; set; }

        /// <summary>
        /// Returns true if this game is DLC/expansion content
        /// </summary>
        [MemberwiseEqualityIgnore]
        public bool IsDlc => GameType.IsDlc();

        /// <summary>
        /// Returns true if this game is available on PC (Windows/Linux/Mac)
        /// </summary>
        [MemberwiseEqualityIgnore]
        public bool IsOnPc => Platforms?.Any(p =>
            p.Family == PlatformFamily.PC ||
            p.Family == PlatformFamily.Linux ||
            p.Family == PlatformFamily.Mac ||
            p.IgdbId == GamePlatform.CommonPlatforms.Windows ||
            p.IgdbId == GamePlatform.CommonPlatforms.Linux ||
            p.IgdbId == GamePlatform.CommonPlatforms.Mac) ?? false;

        /// <summary>
        /// Gets the primary platform family (PC prioritized)
        /// </summary>
        [MemberwiseEqualityIgnore]
        public PlatformFamily PrimaryPlatformFamily
        {
            get
            {
                if (Platforms == null || !Platforms.Any())
                {
                    return PlatformFamily.Unknown;
                }

                // Prioritize PC
                if (IsOnPc)
                {
                    return PlatformFamily.PC;
                }

                return Platforms.FirstOrDefault()?.Family ?? PlatformFamily.Unknown;
            }
        }

        [MemberwiseEqualityIgnore]
        public bool IsRecentGame
        {
            get
            {
                if ((PhysicalRelease.HasValue && PhysicalRelease.Value >= DateTime.UtcNow.AddDays(-21)) ||
                    (DigitalRelease.HasValue && DigitalRelease.Value >= DateTime.UtcNow.AddDays(-21)) ||
                    (InDevelopment.HasValue && InDevelopment.Value >= DateTime.UtcNow.AddDays(-120)))
                {
                    return true;
                }

                return false;
            }
        }

        public DateTime PhysicalReleaseDate()
        {
            return PhysicalRelease ?? (InDevelopment?.AddDays(90) ?? DateTime.MaxValue);
        }
    }
}
