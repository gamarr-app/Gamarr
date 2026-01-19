using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles
{
    public class GameFileMoveResult
    {
        public GameFileMoveResult()
        {
            OldFiles = new List<DeletedGameFile>();
        }

        public GameFile GameFile { get; set; }
        public List<DeletedGameFile> OldFiles { get; set; }
    }
}
