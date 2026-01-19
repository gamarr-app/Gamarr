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

#pragma warning disable CS0618 // Disable obsolete warnings for ImdbId property

namespace Gamarr.Api.V3.Games
{
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
        /// DEPRECATED: IMDb is a movie database and does not apply to games.
        /// This field is kept for backwards compatibility but will always be null/empty for games.
        /// </summary>
        [Obsolete("IMDb is a movie database and does not apply to games. This field will always be null/empty.")]
        public string ImdbId { get; set; }

        public int IgdbId { get; set; }
        public int SteamAppId { get; set; }
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
        public DateTime? LastSearchTime { get; set; }
        public GameStatisticsResource Statistics { get; set; }

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
                IgdbId = model.IgdbId,
                SteamAppId = model.GameMetadata.Value.SteamAppId,
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
                ImdbId = model.ImdbId,
                TitleSlug = model.GameMetadata.Value.IgdbId.ToString(),
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
                LastSearchTime = model.LastSearchTime,
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
                    IgdbId = resource.IgdbId,
                    SteamAppId = resource.SteamAppId,
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
                    ImdbId = resource.ImdbId,
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
