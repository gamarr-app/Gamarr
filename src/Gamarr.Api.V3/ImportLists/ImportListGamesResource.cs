using System;
using System.Collections.Generic;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.ImportLists
{
    public class ImportListGamesResource : RestResource
    {
        public ImportListGamesResource()
        {
            Lists = new HashSet<int>();
        }

        public string Title { get; set; }
        public string SortTitle { get; set; }
        public Language OriginalLanguage { get; set; }
        public GameStatusType Status { get; set; }
        public string Overview { get; set; }
        public DateTime? InDevelopment { get; set; }
        public DateTime? PhysicalRelease { get; set; }
        public DateTime? DigitalRelease { get; set; }
        public List<MediaCover> Images { get; set; }
        public string Website { get; set; }
        public string RemotePoster { get; set; }
        public int Year { get; set; }
        public string YouTubeTrailerId { get; set; }
        public string Studio { get; set; }

        public int Runtime { get; set; }
        public string ImdbId { get; set; }
        public int IgdbId { get; set; }
        public string Folder { get; set; }
        public string Certification { get; set; }
        public float Popularity { get; set; }
        public List<string> Genres { get; set; }
        public Ratings Ratings { get; set; }
        public GameCollection Collection { get; set; }
        public bool IsExcluded { get; set; }
        public bool IsExisting { get; set; }
        public bool IsTrending { get; set; }
        public bool IsPopular { get; set; }

        public bool IsRecommendation { get; set; }
        public HashSet<int> Lists { get; set; }
    }

    public static class DiscoverGamesResourceMapper
    {
        public static ImportListGamesResource ToResource(this Game model)
        {
            if (model == null)
            {
                return null;
            }

            return new ImportListGamesResource
            {
                IgdbId = model.IgdbId,
                Title = model.Title,
                SortTitle = model.GameMetadata.Value.SortTitle,
                OriginalLanguage = model.GameMetadata.Value.OriginalLanguage,
                InDevelopment = model.GameMetadata.Value.InDevelopment,
                PhysicalRelease = model.GameMetadata.Value.PhysicalRelease,
                DigitalRelease = model.GameMetadata.Value.DigitalRelease,

                Status = model.GameMetadata.Value.Status,
                Overview = model.GameMetadata.Value.Overview,

                Images = model.GameMetadata.Value.Images,

                Year = model.Year,

                Runtime = model.GameMetadata.Value.Runtime,
                ImdbId = model.ImdbId,
                Certification = model.GameMetadata.Value.Certification,
                Website = model.GameMetadata.Value.Website,
                Genres = model.GameMetadata.Value.Genres,
                Ratings = model.GameMetadata.Value.Ratings,
                Popularity = model.GameMetadata.Value.Popularity,
                YouTubeTrailerId = model.GameMetadata.Value.YouTubeTrailerId,
                Collection = new GameCollection { Title = model.GameMetadata.Value.CollectionTitle, IgdbId = model.GameMetadata.Value.CollectionIgdbId },
                Studio = model.GameMetadata.Value.Studio
            };
        }

        public static ImportListGamesResource ToResource(this ImportListGame model)
        {
            if (model == null)
            {
                return null;
            }

            return new ImportListGamesResource
            {
                IgdbId = model.IgdbId,
                Title = model.Title,
                SortTitle = model.GameMetadata.Value.SortTitle,
                OriginalLanguage = model.GameMetadata.Value.OriginalLanguage,
                InDevelopment = model.GameMetadata.Value.InDevelopment,
                PhysicalRelease = model.GameMetadata.Value.PhysicalRelease,
                DigitalRelease = model.GameMetadata.Value.DigitalRelease,

                Status = model.GameMetadata.Value.Status,
                Overview = model.GameMetadata.Value.Overview,

                Images = model.GameMetadata.Value.Images,

                Year = model.Year,

                Runtime = model.GameMetadata.Value.Runtime,
                ImdbId = model.ImdbId,
                Certification = model.GameMetadata.Value.Certification,
                Website = model.GameMetadata.Value.Website,
                Genres = model.GameMetadata.Value.Genres,
                Ratings = model.GameMetadata.Value.Ratings,
                YouTubeTrailerId = model.GameMetadata.Value.YouTubeTrailerId,
                Popularity = model.GameMetadata.Value.Popularity,
                Studio = model.GameMetadata.Value.Studio,
                Collection = new GameCollection { Title = model.GameMetadata.Value.CollectionTitle, IgdbId = model.GameMetadata.Value.CollectionIgdbId },
                Lists = new HashSet<int> { model.ListId }
            };
        }
    }
}
