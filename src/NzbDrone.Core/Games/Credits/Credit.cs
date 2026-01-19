using System.Collections.Generic;

namespace NzbDrone.Core.Games.Credits
{
    public class Credit : Entity<Credit>
    {
        public Credit()
        {
            Images = new List<MediaCover.MediaCover>();
        }

        public string Name { get; set; }
        public string CreditIgdbId { get; set; }
        public int PersonIgdbId { get; set; }
        public int GameMetadataId { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public string Department { get; set; }
        public string Job { get; set; }
        public string Character { get; set; }
        public int Order { get; set; }
        public CreditType Type { get; set; }
    }

    public enum CreditType
    {
        Cast,
        Crew
    }
}
