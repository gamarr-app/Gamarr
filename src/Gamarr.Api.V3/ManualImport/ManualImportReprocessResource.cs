using System.Collections.Generic;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Qualities;
using Gamarr.Api.V3.CustomFormats;
using Gamarr.Api.V3.Games;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.ManualImport
{
    public class ManualImportReprocessResource : RestResource
    {
        public string Path { get; set; }
        public int GameId { get; set; }
        public GameResource Game { get; set; }
        public QualityModel Quality { get; set; }
        public List<Language> Languages { get; set; }
        public string ReleaseGroup { get; set; }
        public string DownloadId { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public int IndexerFlags { get; set; }
        public IEnumerable<ImportRejectionResource> Rejections { get; set; }
    }
}
