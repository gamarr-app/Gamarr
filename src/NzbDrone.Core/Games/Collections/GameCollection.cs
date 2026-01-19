using System;
using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Games.Collections
{
    public class GameCollection : ModelBase
    {
        public GameCollection()
        {
            Images = new List<MediaCover.MediaCover>();
            Tags = new HashSet<int>();
        }

        public string Title { get; set; }
        public string CleanTitle { get; set; }
        public string SortTitle { get; set; }
        public int IgdbId { get; set; }
        public string Overview { get; set; }
        public bool Monitored { get; set; }
        public int QualityProfileId { get; set; }
        public string RootFolderPath { get; set; }
        public bool SearchOnAdd { get; set; }
        public GameStatusType MinimumAvailability { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public DateTime Added { get; set; }
        public List<GameMetadata> Games { get; set; }
        public HashSet<int> Tags { get; set; }

        public void ApplyChanges(GameCollection otherCollection)
        {
            IgdbId = otherCollection.IgdbId;

            Monitored = otherCollection.Monitored;
            SearchOnAdd = otherCollection.SearchOnAdd;
            QualityProfileId = otherCollection.QualityProfileId;
            MinimumAvailability = otherCollection.MinimumAvailability;
            RootFolderPath = otherCollection.RootFolderPath;
            Tags = otherCollection.Tags;
        }
    }
}
