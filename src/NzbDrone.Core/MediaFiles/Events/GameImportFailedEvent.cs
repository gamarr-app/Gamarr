using System;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameImportFailedEvent : IEvent
    {
        public Exception Exception { get; set; }
        public LocalGame GameInfo { get; }
        public bool NewDownload { get; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; }
        public string DownloadId { get; }

        public GameImportFailedEvent(Exception exception, LocalGame gameInfo, bool newDownload, DownloadClientItem downloadClientItem)
        {
            Exception = exception;
            GameInfo = gameInfo;
            NewDownload = newDownload;

            if (downloadClientItem != null)
            {
                DownloadClientInfo = downloadClientItem.DownloadClientInfo;
                DownloadId = downloadClientItem.DownloadId;
            }
        }
    }
}
