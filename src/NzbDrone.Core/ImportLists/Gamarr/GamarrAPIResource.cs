using System;
using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists.Gamarr
{
    public class GamarrGame
    {
        public string Title { get; set; }
        public string SortTitle { get; set; }
        public int IgdbId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public DateTime InDevelopment { get; set; }
        public DateTime PhysicalRelease { get; set; }
        public int Year { get; set; }
        public string TitleSlug { get; set; }
        public int QualityProfileId { get; set; }
        public string Path { get; set; }
        public HashSet<int> Tags { get; set; }
    }

    public class GamarrProfile
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class GamarrTag
    {
        public string Label { get; set; }
        public int Id { get; set; }
    }

    public class GamarrRootFolder
    {
        public string Path { get; set; }
        public int Id { get; set; }
    }
}
