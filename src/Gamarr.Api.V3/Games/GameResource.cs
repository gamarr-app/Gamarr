using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using Gamarr.Api.V3.GameFiles;
using Gamarr.Http.REST;
using Swashbuckle.AspNetCore.Annotations;

namespace Gamarr.Api.V3.Games
{
    /// <summary>
    /// Lightweight summary of a game for parent/DLC references
    /// </summary>
    public class GameSummaryResource
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int SteamAppId { get; set; }
        public int IgdbId { get; set; }
        public string TitleSlug { get; set; }
        public List<MediaCover> Images { get; set; }
    }

    public class GameResource : RestResource
    {
        public GameResource()
        {
            Monitored = true;
            MinimumAvailability = GameStatusType.Released;
        }

        // Todo: Sorters should be done completely on the client
        // Todo: Is there an easy way to keep IgnoreArticlesWhenSorting in sync between, Series, History, Missing?
        // Todo: We should get the entire Profile instead of ID and Name separately

        // View Only
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public Language OriginalLanguage { get; set; }
        public List<AlternativeTitleResource> AlternateTitles { get; set; }
        public int? SecondaryYear { get; set; }
        public int SecondaryYearSourceId { get; set; }
        public string SortTitle { get; set; }
        public long? SizeOnDisk { get; set; }
        public GameStatusType Status { get; set; }
        public string Overview { get; set; }
        public DateTime? EarlyAccess { get; set; }
        public DateTime? PhysicalRelease { get; set; }
        public DateTime? DigitalRelease { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string PhysicalReleaseNote { get; set; }
        public List<MediaCover> Images { get; set; }
        public string Website { get; set; }

        // public bool Downloaded { get; set; }
        public string RemotePoster { get; set; }
        public int Year { get; set; }
        public string YouTubeTrailerId { get; set; }
        public string Studio { get; set; }

        // View & Edit
        public string Path { get; set; }
        public int QualityProfileId { get; set; }

        // Compatibility
        public bool? HasFile { get; set; }
        public int GameFileId { get; set; }

        // Editing Only
        public bool Monitored { get; set; }
        public GameStatusType MinimumAvailability { get; set; }
        public bool IsAvailable { get; set; }
        public string FolderName { get; set; }

        public int Runtime { get; set; }
        public string CleanTitle { get; set; }

        /// <summary>
        /// Primary identifier - Steam App ID
        /// </summary>
        public int SteamAppId { get; set; }

        /// <summary>
        /// Secondary identifier - IGDB ID (for metadata enrichment)
        /// </summary>
        public int IgdbId { get; set; }

        /// <summary>
        /// IGDB slug for URL generation (e.g., "half-life-2")
        /// </summary>
        public string IgdbSlug { get; set; }

        /// <summary>
        /// Secondary identifier - RAWG ID (for metadata enrichment)
        /// </summary>
        public int RawgId { get; set; }

        public string TitleSlug { get; set; }
        public string RootFolderPath { get; set; }
        public string Folder { get; set; }
        public string Certification { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Keywords { get; set; }
        public HashSet<int> Tags { get; set; }
        public DateTime Added { get; set; }
        public AddGameOptions AddOptions { get; set; }
        public Ratings Ratings { get; set; }
        public GameFileResource GameFile { get; set; }
        public GameCollectionResource Collection { get; set; }
        public float Popularity { get; set; }
        public List<int> Recommendations { get; set; }
        public DateTime? LastSearchTime { get; set; }
        public GameStatisticsResource Statistics { get; set; }

        // DLC-related properties

        /// <summary>
        /// Type of game content (MainGame, DLC, Expansion, etc.)
        /// </summary>
        public GameType GameType { get; set; }

        /// <summary>
        /// Display name for the game type
        /// </summary>
        public string GameTypeDisplayName { get; set; }

        /// <summary>
        /// Whether this is DLC/expansion content
        /// </summary>
        public bool IsDlc { get; set; }

        /// <summary>
        /// IGDB ID of the parent game (for DLCs/expansions)
        /// </summary>
        public int? ParentGameIgdbId { get; set; }

        /// <summary>
        /// Parent game info (if this is a DLC)
        /// </summary>
        public GameSummaryResource ParentGame { get; set; }

        /// <summary>
        /// List of IGDB IDs for DLCs/expansions of this game
        /// </summary>
        public List<int> DlcIds { get; set; }

        /// <summary>
        /// Number of known DLCs for this game
        /// </summary>
        public int DlcCount { get; set; }

        // Hiding this so people don't think its usable (only used to set the initial state)
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [SwaggerIgnore]
        public bool Grabbed { get; set; }

        // Hiding this so people don't think its usable (only used for searches)
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [SwaggerIgnore]
        public bool IsExcluded { get; set; }
    }

    public static class GameResourceMapper
    {
        public static GameResource ToResource(this Game model, int availDelay, GameTranslation gameTranslation = null, IUpgradableSpecification upgradableSpecification = null, ICustomFormatCalculationService formatCalculationService = null)
        {
            if (model == null)
            {
                return null;
            }

            var gameFile = model.GameFile?.ToResource(model, upgradableSpecification, formatCalculationService);

            var translatedTitle = gameTranslation?.Title ?? model.Title;
            var translatedOverview = gameTranslation?.Overview ?? model.GameMetadata.Value.Overview;

            var collection = model.GameMetadata.Value.CollectionIgdbId > 0 ? new GameCollectionResource { Title = model.GameMetadata.Value.CollectionTitle, IgdbId = model.GameMetadata.Value.CollectionIgdbId } : null;

            return new GameResource
            {
                Id = model.Id,
                SteamAppId = model.SteamAppId,
                IgdbId = model.IgdbId,
                IgdbSlug = model.GameMetadata.Value.IgdbSlug,
                RawgId = model.RawgId,
                Title = translatedTitle,
                OriginalTitle = model.GameMetadata.Value.OriginalTitle,
                OriginalLanguage = model.GameMetadata.Value.OriginalLanguage,
                SortTitle = GameTitleNormalizer.Normalize(translatedTitle, model.IgdbId),
                EarlyAccess = model.GameMetadata.Value.EarlyAccess,
                PhysicalRelease = model.GameMetadata.Value.PhysicalRelease,
                DigitalRelease = model.GameMetadata.Value.DigitalRelease,
                ReleaseDate = model.GetReleaseDate(),

                Status = model.GameMetadata.Value.Status,
                Overview = translatedOverview,

                Images = model.GameMetadata.Value.Images.JsonClone(),

                Year = model.Year,
                SecondaryYear = model.GameMetadata.Value.SecondaryYear,

                GameFileId = model.GameFileId,

                Path = model.Path,
                QualityProfileId = model.QualityProfileId,

                Monitored = model.Monitored,
                MinimumAvailability = model.MinimumAvailability,

                IsAvailable = model.IsAvailable(availDelay),
                FolderName = model.FolderName(),

                Runtime = model.GameMetadata.Value.Runtime,
                CleanTitle = model.GameMetadata.Value.CleanTitle,
                TitleSlug = model.SteamAppId > 0 ? model.SteamAppId.ToString() :
                            model.IgdbId > 0 ? model.IgdbId.ToString() :
                            model.RawgId > 0 ? $"rawg-{model.RawgId}" :
                            model.Id.ToString(),
                RootFolderPath = model.RootFolderPath,
                Certification = model.GameMetadata.Value.Certification,
                Website = model.GameMetadata.Value.Website,
                Genres = model.GameMetadata.Value.Genres,
                Keywords = model.GameMetadata.Value.Keywords,
                Tags = model.Tags,
                Added = model.Added,
                AddOptions = model.AddOptions,
                AlternateTitles = model.GameMetadata.Value.AlternativeTitles.ToResource(),
                Ratings = model.GameMetadata.Value.Ratings,
                GameFile = gameFile,
                YouTubeTrailerId = model.GameMetadata.Value.YouTubeTrailerId,
                Studio = model.GameMetadata.Value.Studio,
                Collection = collection,
                Popularity = model.GameMetadata.Value.Popularity,
                Recommendations = model.GameMetadata.Value.Recommendations ?? new List<int>(),
                LastSearchTime = model.LastSearchTime,

                // DLC properties
                GameType = model.GameMetadata.Value.GameType,
                GameTypeDisplayName = model.GameMetadata.Value.GameType.GetDisplayName(),
                IsDlc = model.GameMetadata.Value.IsDlc,
                ParentGameIgdbId = model.GameMetadata.Value.ParentGameId,
                DlcIds = model.GameMetadata.Value.DlcIds ?? new List<int>(),
                DlcCount = model.GameMetadata.Value.DlcIds?.Count ?? 0,
            };
        }

        public static Game ToModel(this GameResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new Game
            {
                Id = resource.Id,

                GameMetadata = new GameMetadata
                {
                    SteamAppId = resource.SteamAppId,
                    IgdbId = resource.IgdbId,
                    IgdbSlug = resource.IgdbSlug,
                    RawgId = resource.RawgId,
                    Title = resource.Title,
                    Genres = resource.Genres,
                    Images = resource.Images,
                    OriginalTitle = resource.OriginalTitle,
                    SortTitle = resource.SortTitle,
                    EarlyAccess = resource.EarlyAccess,
                    PhysicalRelease = resource.PhysicalRelease,
                    Year = resource.Year,
                    SecondaryYear = resource.SecondaryYear,
                    Overview = resource.Overview,
                    Certification = resource.Certification,
                    Website = resource.Website,
                    Ratings = resource.Ratings,
                    YouTubeTrailerId = resource.YouTubeTrailerId,
                    Studio = resource.Studio,
                    Runtime = resource.Runtime,
                    CleanTitle = resource.CleanTitle,
                    GameType = resource.GameType,
                    ParentGameId = resource.ParentGameIgdbId,
                    DlcIds = resource.DlcIds ?? new List<int>(),
                },

                Path = resource.Path,
                QualityProfileId = resource.QualityProfileId,

                Monitored = resource.Monitored,
                MinimumAvailability = resource.MinimumAvailability,

                RootFolderPath = resource.RootFolderPath,

                Tags = resource.Tags ?? new HashSet<int>(),
                Added = resource.Added,
                AddOptions = resource.AddOptions
            };
        }

        public static Game ToModel(this GameResource resource, Game game)
        {
            var updatedGame = resource.ToModel();

            game.ApplyChanges(updatedGame);

            return game;
        }

        public static List<GameResource> ToResource(this IEnumerable<Game> games, int availDelay, IUpgradableSpecification upgradableSpecification = null, ICustomFormatCalculationService formatCalculationService = null)
        {
            return games.Select(x => ToResource(x, availDelay, null, upgradableSpecification, formatCalculationService)).ToList();
        }

        public static List<Game> ToModel(this IEnumerable<GameResource> resources)
        {
            return resources.Select(ToModel).ToList();
        }
    }
}
