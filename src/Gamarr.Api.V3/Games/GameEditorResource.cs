using System.Collections.Generic;
using NzbDrone.Core.Games;

namespace Gamarr.Api.V3.Games
{
    public class GameEditorResource
    {
        public List<int> GameIds { get; set; }
        public bool? Monitored { get; set; }
        public int? QualityProfileId { get; set; }
        public GameStatusType? MinimumAvailability { get; set; }
        public string RootFolderPath { get; set; }
        public List<int> Tags { get; set; }
        public ApplyTags ApplyTags { get; set; }
        public bool MoveFiles { get; set; }
        public bool DeleteFiles { get; set; }
        public bool AddImportExclusion { get; set; }
    }
}
