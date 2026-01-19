using System.Collections.Generic;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public class CustomFormatInput
    {
        public ParsedGameInfo GameInfo { get; set; }
        public Game Game { get; set; }
        public long Size { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public List<Language> Languages { get; set; }
        public string Filename { get; set; }

        public CustomFormatInput()
        {
            Languages = new List<Language>();
        }
    }
}
