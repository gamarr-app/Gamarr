using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Games;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public interface IFailedDownloadService
    {
        void MarkAsFailed(int historyId, bool skipRedownload = false, bool deleteFiles = false);
        void MarkAsFailed(TrackedDownload trackedDownload, bool skipRedownload = false);
        void Check(TrackedDownload trackedDownload);
        void ProcessFailed(TrackedDownload trackedDownload);
    }

    public class FailedDownloadService : IFailedDownloadService
    {
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDownloadClientFactory _downloadClientFactory;
        private readonly ITrackedDownloadService _trackedDownloadService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IGameService _gameService;
        private readonly Logger _logger;

        public FailedDownloadService(IHistoryService historyService,
                                     IEventAggregator eventAggregator,
                                     IDownloadClientFactory downloadClientFactory,
                                     ITrackedDownloadService trackedDownloadService,
                                     IMediaFileService mediaFileService,
                                     IDeleteMediaFiles mediaFileDeletionService,
                                     IGameService gameService,
                                     Logger logger)
        {
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _downloadClientFactory = downloadClientFactory;
            _trackedDownloadService = trackedDownloadService;
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _gameService = gameService;
            _logger = logger;
        }

        public void MarkAsFailed(int historyId, bool skipRedownload = false, bool deleteFiles = false)
        {
            var history = _historyService.Get(historyId);

            var downloadId = history.DownloadId;

            if (downloadId.IsNullOrWhiteSpace())
            {
                PublishDownloadFailedEvent(history, "Manually marked as failed", skipRedownload: skipRedownload);

                return;
            }

            if (deleteFiles)
            {
                DeleteImportedFiles(downloadId);
            }

            // Look the download up in the client before publishing. Without a TrackedDownload
            // attached, DownloadEventHub.Handle(DownloadFailedEvent) returns at its null check
            // and nothing is ever removed from the download client, so a release failed from a
            // history row left the torrent seeding and the data on disk -- the user then had to
            // delete it by hand. BlocklistService also falls back to the history payload for the
            // infohash when there is no tracked download, which pushes and most Torznab grabs
            // never record.
            var trackedDownload = _trackedDownloadService.Find(downloadId);

            // skipRedownload was being dropped on this path: the flag reached MarkAsFailed and
            // then was never passed on, so RedownloadFailedDownloadService searched again anyway.
            PublishDownloadFailedEvent(history, "Manually marked as failed", trackedDownload, skipRedownload);
        }

        // Everything imported from this download, found through the DownloadFolderImported
        // history rows that record the resulting GameFile id (HistoryService writes "FileId"
        // there). Deleting by download id rather than by game keeps a second, good file for
        // the same game -- a DLC or component slot imported from a different release -- in place.
        private void DeleteImportedFiles(string downloadId)
        {
            var importedItems = _historyService.Find(downloadId, GameHistoryEventType.DownloadFolderImported);

            foreach (var item in importedItems)
            {
                if (!int.TryParse(item.Data.GetValueOrDefault("FileId"), out var fileId) || fileId <= 0)
                {
                    continue;
                }

                try
                {
                    var gameFile = _mediaFileService.GetGame(fileId);

                    if (gameFile == null)
                    {
                        continue;
                    }

                    var game = _gameService.GetGame(item.GameId);

                    _mediaFileDeletionService.DeleteGameFile(game, gameFile);
                }
                catch (Exception e)
                {
                    // A row already deleted by hand, or a game removed from the library, must not
                    // stop the blocklist and the client clean-up that the user actually asked for.
                    _logger.Warn(e, "Couldn't delete game file {0} imported from {1}", fileId, downloadId);
                }
            }
        }

        public void MarkAsFailed(TrackedDownload trackedDownload, bool skipRedownload = false)
        {
            var history = GetGrabbedHistory(trackedDownload.DownloadItem.DownloadId);

            if (history.Any())
            {
                PublishDownloadFailedEvent(history.First(), "Manually marked as failed", trackedDownload, skipRedownload: skipRedownload);
            }
        }

        public void Check(TrackedDownload trackedDownload)
        {
            // Only process tracked downloads that are still downloading or import is blocked (if they fail after attempting to be processed)
            if (trackedDownload.State != TrackedDownloadState.Downloading && trackedDownload.State != TrackedDownloadState.ImportBlocked)
            {
                return;
            }

            if (trackedDownload.DownloadItem.IsEncrypted ||
                trackedDownload.DownloadItem.Status == DownloadItemStatus.Failed)
            {
                var grabbedItems = GetGrabbedHistory(trackedDownload.DownloadItem.DownloadId);

                if (grabbedItems.Empty())
                {
                    trackedDownload.Warn(trackedDownload.DownloadItem.IsEncrypted ? "Download is encrypted and wasn't grabbed by Gamarr, skipping automatic download handling" : "Download has failed wasn't grabbed by Gamarr, skipping automatic download handling");
                    return;
                }

                trackedDownload.State = TrackedDownloadState.FailedPending;
                return;
            }

            CheckForStall(trackedDownload);
        }

        private void CheckForStall(TrackedDownload trackedDownload)
        {
            // Paused and queued downloads aren't expected to make progress, so
            // they can't stall. Actual stalled torrents surface as Downloading
            // or Warning (e.g. qBittorrent's stalledDL) and stay eligible.
            if (trackedDownload.DownloadItem.RemainingSize <= 0 ||
                trackedDownload.DownloadItem.Status is DownloadItemStatus.Completed
                    or DownloadItemStatus.Paused
                    or DownloadItemStatus.Queued)
            {
                return;
            }

            var clientDef = _downloadClientFactory.Find(trackedDownload.DownloadClient);
            var timeoutHours = clientDef?.StallTimeoutHours ?? 0;

            if (timeoutHours <= 0 || trackedDownload.RemainingSizeChangedAt is not { } lastProgressAt)
            {
                return;
            }

            var stalledFor = DateTime.UtcNow - lastProgressAt;
            if (stalledFor < TimeSpan.FromHours(timeoutHours))
            {
                return;
            }

            var grabbedItems = GetGrabbedHistory(trackedDownload.DownloadItem.DownloadId);
            if (grabbedItems.Empty())
            {
                // No grab history — warn but don't fail (we can't blocklist or re-search what we didn't grab).
                trackedDownload.Warn("Download has been stalled for {0:N1} hours, but wasn't grabbed by Gamarr", stalledFor.TotalHours);
                return;
            }

            _logger.Info("Download '{0}' stalled for {1:N1} hours ({2} bytes remaining unchanged); marking as failed", CleanseLogMessage.SanitizeLogParam(trackedDownload.DownloadItem.Title), stalledFor.TotalHours, trackedDownload.LastRemainingSize);

            // Fail() (not a bare state change) also sets CanBeRemoved, without
            // which DownloadEventHub skips RemoveFailedDownloads and the dead
            // torrent stays in the client forever, holding its partial data.
            trackedDownload.Fail();
        }

        public void ProcessFailed(TrackedDownload trackedDownload)
        {
            if (trackedDownload.State != TrackedDownloadState.FailedPending)
            {
                return;
            }

            var grabbedItems = GetGrabbedHistory(trackedDownload.DownloadItem.DownloadId);

            if (grabbedItems.Empty())
            {
                return;
            }

            var failure = "Failed download detected";

            if (trackedDownload.DownloadItem.IsEncrypted)
            {
                failure = "Encrypted download detected";
            }
            else if (trackedDownload.DownloadItem.Status == DownloadItemStatus.Failed && trackedDownload.DownloadItem.Message.IsNotNullOrWhiteSpace())
            {
                failure = trackedDownload.DownloadItem.Message;
            }

            trackedDownload.State = TrackedDownloadState.Failed;
            PublishDownloadFailedEvent(grabbedItems.First(), failure, trackedDownload);
        }

        private void PublishDownloadFailedEvent(GameHistory historyItem, string message, TrackedDownload trackedDownload = null, bool skipRedownload = false)
        {
            Enum.TryParse(historyItem.Data.GetValueOrDefault(GameHistory.RELEASE_SOURCE, ReleaseSourceType.Unknown.ToString()), out ReleaseSourceType releaseSource);

            var downloadFailedEvent = new DownloadFailedEvent
            {
                GameId = historyItem.GameId,
                Quality = historyItem.Quality,
                SourceTitle = historyItem.SourceTitle,
                DownloadClient = historyItem.Data.GetValueOrDefault(GameHistory.DOWNLOAD_CLIENT),
                DownloadId = historyItem.DownloadId,
                Message = message,
                Data = historyItem.Data,
                TrackedDownload = trackedDownload,
                Languages = historyItem.Languages,
                SkipRedownload = skipRedownload,
                ReleaseSource = releaseSource,
            };

            _eventAggregator.PublishEvent(downloadFailedEvent);
        }

        private List<GameHistory> GetGrabbedHistory(string downloadId)
        {
            // Sort by date so items are always in the same order
            return _historyService.Find(downloadId, GameHistoryEventType.Grabbed)
                .OrderByDescending(h => h.Date)
                .ToList();
        }
    }
}
