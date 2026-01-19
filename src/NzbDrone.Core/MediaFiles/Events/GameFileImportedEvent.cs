using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameFileImportedEvent : IEvent
    {
        public LocalGame GameInfo { get; private set; }
        public GameFile ImportedGame { get; private set; }
        public List<DeletedGameFile> OldFiles { get; private set; }
        public bool NewDownload { get; private set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; private set; }

        public GameFileImportedEvent(LocalGame gameInfo, GameFile importedGame, List<DeletedGameFile> oldFiles, bool newDownload, DownloadClientItem downloadClientItem)
        {
            GameInfo = gameInfo;
            ImportedGame = importedGame;
            OldFiles = oldFiles;
            NewDownload = newDownload;

            if (downloadClientItem != null)
            {
                DownloadClientInfo = downloadClientItem.DownloadClientInfo;
                DownloadId = downloadClientItem.DownloadId;
            }
        }
    }
}
