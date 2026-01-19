using System.Collections.Generic;
using NzbDrone.Core.Games;

namespace Gamarr.Api.V3.Collections
{
    public class CollectionUpdateResource
    {
        public List<int> CollectionIds { get; set; }
        public bool? Monitored { get; set; }
        public bool? MonitorGames { get; set; }
        public bool? SearchOnAdd { get; set; }
        public int? QualityProfileId { get; set; }
        public string RootFolderPath { get; set; }
        public GameStatusType? MinimumAvailability { get; set; }
    }
}
