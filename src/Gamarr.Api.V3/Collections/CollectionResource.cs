using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Collections
{
    public class CollectionResource : RestResource
    {
        public CollectionResource()
        {
            Games = new List<CollectionGameResource>();
        }

        public string Title { get; set; }
        public string SortTitle { get; set; }
        public int IgdbId { get; set; }
        public List<MediaCover> Images { get; set; }
        public string Overview { get; set; }
        public bool Monitored { get; set; }
        public string RootFolderPath { get; set; }
        public int QualityProfileId { get; set; }
        public bool SearchOnAdd { get; set; }
        public GameStatusType MinimumAvailability { get; set; }
        public List<CollectionGameResource> Games { get; set; }
        public int MissingGames { get; set; }
        public HashSet<int> Tags { get; set; }
    }

    public static class CollectionResourceMapper
    {
        public static CollectionResource ToResource(this GameCollection model)
        {
            if (model == null)
            {
                return null;
            }

            return new CollectionResource
            {
                Id = model.Id,
                IgdbId = model.IgdbId,
                Title = model.Title,
                Overview = model.Overview,
                SortTitle = model.SortTitle,
                Monitored = model.Monitored,
                Images = model.Images,
                QualityProfileId = model.QualityProfileId,
                RootFolderPath = model.RootFolderPath,
                MinimumAvailability = model.MinimumAvailability,
                SearchOnAdd = model.SearchOnAdd,
                Tags = model.Tags
            };
        }

        public static List<CollectionResource> ToResource(this IEnumerable<GameCollection> collections)
        {
            return collections.Select(ToResource).ToList();
        }

        public static GameCollection ToModel(this CollectionResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new GameCollection
            {
                Id = resource.Id,
                Title = resource.Title,
                IgdbId = resource.IgdbId,
                SortTitle = resource.SortTitle,
                Overview = resource.Overview,
                Monitored = resource.Monitored,
                QualityProfileId = resource.QualityProfileId,
                RootFolderPath = resource.RootFolderPath,
                SearchOnAdd = resource.SearchOnAdd,
                MinimumAvailability = resource.MinimumAvailability,
                Tags = resource.Tags
            };
        }

        public static GameCollection ToModel(this CollectionResource resource, GameCollection collection)
        {
            var updatedgame = resource.ToModel();

            collection.ApplyChanges(updatedgame);

            return collection;
        }
    }
}
