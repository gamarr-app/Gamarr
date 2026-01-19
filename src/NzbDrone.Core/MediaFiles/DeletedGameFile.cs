namespace NzbDrone.Core.MediaFiles
{
    public class DeletedGameFile
    {
        public string RecycleBinPath { get; set; }
        public GameFile GameFile { get; set; }

        public DeletedGameFile(GameFile gameFile, string recycleBinPath)
        {
            GameFile = gameFile;
            RecycleBinPath = recycleBinPath;
        }
    }
}
