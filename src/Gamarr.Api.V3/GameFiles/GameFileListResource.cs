using System.Collections.Generic;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Qualities;

namespace Gamarr.Api.V3.GameFiles
{
    public class GameFileListResource
    {
        public List<int> GameFileIds { get; set; } = new ();
        public List<Language> Languages { get; set; }
        public QualityModel Quality { get; set; }
        public string Edition { get; set; }
        public string ReleaseGroup { get; set; }
        public string SceneName { get; set; }
        public int? IndexerFlags { get; set; }
    }
}
