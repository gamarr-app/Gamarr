namespace NzbDrone.Core.MediaFiles
{
    public class RenamedGameFile
    {
        public GameFile GameFile { get; set; }
        public string PreviousPath { get; set; }
        public string PreviousRelativePath { get; set; }
    }
}
