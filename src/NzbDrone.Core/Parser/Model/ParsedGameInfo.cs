using System.Collections.Generic;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedGameInfo
    {
        public ParsedGameInfo()
        {
            GameTitles = new List<string>();
            Languages = new List<Language>();
        }

        public List<string> GameTitles { get; set; }
        public string OriginalTitle { get; set; }
        public string ReleaseTitle { get; set; }
        public string SimpleReleaseTitle { get; set; }
        public QualityModel Quality { get; set; }
        public List<Language> Languages { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }
        public string Edition { get; set; }
        public int Year { get; set; }
        public string ImdbId { get; set; }
        public int IgdbId { get; set; }
        public string HardcodedSubs { get; set; }

        public string GameTitle => PrimaryGameTitle;

        public string PrimaryGameTitle
        {
            get
            {
                if (GameTitles.Count > 0)
                {
                    return GameTitles[0];
                }

                return null;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} {2}", PrimaryGameTitle, Year, Quality);
        }
    }
}
