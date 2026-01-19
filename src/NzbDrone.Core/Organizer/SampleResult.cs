using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Organizer
{
    public class SampleResult
    {
        public string FileName { get; set; }
        public Game Game { get; set; }
        public GameFile GameFile { get; set; }
    }
}
