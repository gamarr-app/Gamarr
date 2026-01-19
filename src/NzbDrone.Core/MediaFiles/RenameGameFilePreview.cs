namespace NzbDrone.Core.MediaFiles
{
    public class RenameGameFilePreview
    {
        public int GameId { get; set; }
        public int GameFileId { get; set; }
        public string ExistingPath { get; set; }
        public string NewPath { get; set; }
    }
}
