using System.Collections.Generic;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Notifications
{
    public class DownloadMessage
    {
        public string Message { get; set; }
        public Game Game { get; set; }
        public LocalGame GameInfo { get; set; }
        public GameFile GameFile { get; set; }
        public List<DeletedGameFile> OldGameFiles { get; set; }
        public string SourcePath { get; set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; set; }
        public GrabbedReleaseInfo Release { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
