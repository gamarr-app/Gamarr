using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadService
    {
        TrackedDownload Find(string downloadId);
        void StopTracking(string downloadId);
        void StopTracking(List<string> downloadIds);
        TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem);
        List<TrackedDownload> GetTrackedDownloads();
        void UpdateTrackable(List<TrackedDownload> trackedDownloads);
    }

    public class TrackedDownloadService : ITrackedDownloadService,
                                          IHandle<GameGrabbedEvent>,
                                          IHandle<GameAddedEvent>,
                                          IHandle<GameEditedEvent>,
                                          IHandle<GamesBulkEditedEvent>,
                                          IHandle<GamesDeletedEvent>
    {
        private readonly IParsingService _parsingService;
        private readonly IHistoryService _historyService;
        private readonly IDownloadHistoryService _downloadHistoryService;
        private readonly IRemoteGameAggregationService _aggregationService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        private readonly ICached<TrackedDownload> _cache;

        public TrackedDownloadService(IParsingService parsingService,
                                      IHistoryService historyService,
                                      IDownloadHistoryService downloadHistoryService,
                                      IRemoteGameAggregationService aggregationService,
                                      ICustomFormatCalculationService formatCalculator,
                                      IEventAggregator eventAggregator,
                                      ICacheManager cacheManager,
                                      Logger logger)
        {
            _parsingService = parsingService;
            _historyService = historyService;
            _downloadHistoryService = downloadHistoryService;
            _aggregationService = aggregationService;
            _formatCalculator = formatCalculator;
            _eventAggregator = eventAggregator;
            _logger = logger;

            _cache = cacheManager.GetCache<TrackedDownload>(GetType());
        }

        public TrackedDownload Find(string downloadId)
        {
            return _cache.Find(downloadId);
        }

        public void StopTracking(string downloadId)
        {
            var trackedDownload = _cache.Find(downloadId);

            _cache.Remove(downloadId);
            _eventAggregator.PublishEvent(new TrackedDownloadsRemovedEvent(new List<TrackedDownload> { trackedDownload }));
        }

        public void StopTracking(List<string> downloadIds)
        {
            var trackedDownloads = new List<TrackedDownload>();

            foreach (var downloadId in downloadIds)
            {
                var trackedDownload = _cache.Find(downloadId);

                _cache.Remove(downloadId);
                trackedDownloads.Add(trackedDownload);
            }

            _eventAggregator.PublishEvent(new TrackedDownloadsRemovedEvent(trackedDownloads));
        }

        public TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem)
        {
            var existingItem = Find(downloadItem.DownloadId);

            if (existingItem != null && existingItem.State != TrackedDownloadState.Downloading)
            {
                LogItemChange(existingItem, existingItem.DownloadItem, downloadItem);

                existingItem.DownloadItem = downloadItem;
                existingItem.IsTrackable = true;
                UpdateStallTracking(existingItem, downloadItem);

                return existingItem;
            }

            var trackedDownload = new TrackedDownload
            {
                DownloadClient = downloadClient.Id,
                DownloadItem = downloadItem,
                Protocol = downloadClient.Protocol,
                IsTrackable = true,
                HasNotifiedManualInteractionRequired = existingItem?.HasNotifiedManualInteractionRequired ?? false,
                LastRemainingSize = existingItem?.LastRemainingSize ?? downloadItem.RemainingSize,
                RemainingSizeChangedAt = existingItem?.RemainingSizeChangedAt ?? DateTime.UtcNow
            };

            UpdateStallTracking(trackedDownload, downloadItem);

            ResolveRemoteGame(trackedDownload, updateState: true);

            LogItemChange(trackedDownload, existingItem?.DownloadItem, trackedDownload.DownloadItem);

            _cache.Set(trackedDownload.DownloadItem.DownloadId, trackedDownload);
            return trackedDownload;
        }

        public List<TrackedDownload> GetTrackedDownloads()
        {
            return _cache.Values.ToList();
        }

        public void UpdateTrackable(List<TrackedDownload> trackedDownloads)
        {
            var untrackable = GetTrackedDownloads().ExceptBy(t => t.DownloadItem.DownloadId, trackedDownloads, t => t.DownloadItem.DownloadId, StringComparer.CurrentCulture).ToList();

            foreach (var trackedDownload in untrackable)
            {
                trackedDownload.IsTrackable = false;
            }
        }

        private void LogItemChange(TrackedDownload trackedDownload, DownloadClientItem existingItem, DownloadClientItem downloadItem)
        {
            if (existingItem == null ||
                existingItem.Status != downloadItem.Status ||
                existingItem.CanBeRemoved != downloadItem.CanBeRemoved ||
                existingItem.CanMoveFiles != downloadItem.CanMoveFiles)
            {
                _logger.Debug("Tracking '{0}:{1}': ClientState={2}{3} GamarrStage={4} Game='{5}' OutputPath={6}.",
                    downloadItem.DownloadClientInfo.Name,
                    downloadItem.Title,
                    downloadItem.Status,
                    downloadItem.CanBeRemoved ? "" : downloadItem.CanMoveFiles ? " (busy)" : " (readonly)",
                    trackedDownload.State,
                    trackedDownload.RemoteGame?.ParsedGameInfo,
                    downloadItem.OutputPath);
            }
        }

        // The in-place re-resolve, run when the library changes under a download that is
        // already being tracked. It deliberately does NOT re-derive State: the download's
        // lifecycle stage is not a function of the library, and rewinding an Imported item
        // to Downloading because someone edited a game would re-run the import pipeline.
        private void UpdateCachedItem(TrackedDownload trackedDownload)
        {
            ResolveRemoteGame(trackedDownload, updateState: false);
        }

        /// <summary>
        /// The single place a tracked download's game is resolved.
        ///
        /// There used to be two: this one, and a title-only copy in <see cref="UpdateCachedItem"/>
        /// with no grab-history fallback. Every game add/edit/bulk-edit/delete runs that path, so
        /// editing a game re-parsed every tracked download title-only and **nulled out any
        /// resolution that only history had established** — the download client reports a
        /// URL-encoded name (`Lords+of+Thunder+(E)[SEGA+CD]`) that the parser rejects, while the
        /// grab history stores the decoded one that parses. The next download-client refresh
        /// repaired it, so the damage was a ~1 minute window of a game vanishing from the queue,
        /// with no error anywhere. Two implementations of "resolve a tracked download" is how one
        /// of them ended up with a safety net and one without; hence one method.
        /// </summary>
        private void ResolveRemoteGame(TrackedDownload trackedDownload, bool updateState)
        {
            var downloadItem = trackedDownload.DownloadItem;

            try
            {
                var downloadHistory = _downloadHistoryService.GetLatestDownloadHistoryItem(downloadItem.DownloadId);

                if (updateState && downloadHistory != null)
                {
                    trackedDownload.State = GetStateFromHistory(downloadHistory.EventType);
                }

                var parsedGameInfo = Parser.Parser.ParseGameTitle(downloadItem.Title);

                trackedDownload.RemoteGame = parsedGameInfo == null
                    ? null
                    : downloadHistory is { EventType: DownloadHistoryEventType.DownloadImported }
                        ? _parsingService.Map(parsedGameInfo, downloadHistory.GameId)
                        : _parsingService.Map(parsedGameInfo, 0, 0, null);

                var historyItems = _historyService.FindByDownloadId(downloadItem.DownloadId)
                    .OrderByDescending(h => h.Date)
                    .ToList();

                if (historyItems.Any())
                {
                    var firstHistoryItem = historyItems.First();
                    var grabbedEvent = historyItems.FirstOrDefault(v => v.EventType == GameHistoryEventType.Grabbed);

                    trackedDownload.Indexer = grabbedEvent?.Data?.GetValueOrDefault("indexer");
                    trackedDownload.Added = grabbedEvent?.Date;

                    // The fallback that the edit path used to be missing. The client's title and
                    // history's SourceTitle are not the same string, and either one can be the
                    // only one that parses.
                    if (trackedDownload.RemoteGame?.Game == null)
                    {
                        var historyGameInfo = Parser.Parser.ParseGameTitle(firstHistoryItem.SourceTitle);

                        if (historyGameInfo != null)
                        {
                            trackedDownload.RemoteGame = _parsingService.Map(historyGameInfo, firstHistoryItem.GameId);
                        }
                    }

                    if (trackedDownload.RemoteGame != null)
                    {
                        trackedDownload.RemoteGame.Release ??= new ReleaseInfo();
                        trackedDownload.RemoteGame.Release.Indexer = trackedDownload.Indexer;
                        trackedDownload.RemoteGame.Release.Title = trackedDownload.RemoteGame.ParsedGameInfo?.ReleaseTitle;

                        if (Enum.TryParse(grabbedEvent?.Data?.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                        {
                            trackedDownload.RemoteGame.Release.IndexerFlags = flags;
                        }

                        if (downloadHistory != null)
                        {
                            trackedDownload.RemoteGame.Release.IndexerId = downloadHistory.IndexerId;
                        }
                    }
                }

                if (trackedDownload.RemoteGame != null)
                {
                    _aggregationService.Augment(trackedDownload.RemoteGame);

                    // Calculate custom formats
                    trackedDownload.RemoteGame.CustomFormats = _formatCalculator.ParseCustomFormat(trackedDownload.RemoteGame, downloadItem.TotalSize);
                }
                else
                {
                    // Track it so it can be displayed in the queue even though we can't determine which game it is for
                    _logger.Trace("No Game found for download '{0}'", downloadItem.Title);
                }
            }
            catch (MultipleGamesFoundException e)
            {
                _logger.Debug(e, "Found multiple games for " + downloadItem.Title);

                trackedDownload.Warn("Unable to import automatically, found multiple games: {0}", string.Join(", ", e.Games));
            }
            catch (Exception e)
            {
                _logger.Debug(e, "Failed to find game for " + downloadItem.Title);

                trackedDownload.Warn("Unable to parse game from title");
            }
        }

        private static void UpdateStallTracking(TrackedDownload trackedDownload, DownloadClientItem downloadItem)
        {
            // Paused/queued items legitimately make no progress; keep advancing
            // the clock so the first refresh after they resume doesn't see
            // hours of "stall" and blocklist a healthy download.
            if (trackedDownload.LastRemainingSize != downloadItem.RemainingSize ||
                downloadItem.Status is DownloadItemStatus.Paused or DownloadItemStatus.Queued)
            {
                trackedDownload.LastRemainingSize = downloadItem.RemainingSize;
                trackedDownload.RemainingSizeChangedAt = DateTime.UtcNow;
            }
        }

        private static TrackedDownloadState GetStateFromHistory(DownloadHistoryEventType eventType)
        {
            switch (eventType)
            {
                case DownloadHistoryEventType.DownloadImported:
                    return TrackedDownloadState.Imported;
                case DownloadHistoryEventType.DownloadFailed:
                    return TrackedDownloadState.Failed;
                case DownloadHistoryEventType.DownloadIgnored:
                    return TrackedDownloadState.Ignored;
                default:
                    return TrackedDownloadState.Downloading;
            }
        }

        public void Handle(GameGrabbedEvent message)
        {
            if (message.DownloadId.IsNullOrWhiteSpace())
            {
                return;
            }

            var trackedDownload = _cache.Find(message.DownloadId);

            if (trackedDownload is { State: TrackedDownloadState.Imported or
                                            TrackedDownloadState.Failed or
                                            TrackedDownloadState.Ignored })
            {
                _cache.Remove(message.DownloadId);
            }
        }

        public void Handle(GameAddedEvent message)
        {
            var cachedItems = _cache.Values
                .Where(t =>
                    t.RemoteGame?.Game == null ||
                    (message.Game?.IgdbId > 0 && message.Game?.IgdbId == t.RemoteGame.Game.IgdbId) ||
                    (message.Game?.SteamAppId > 0 && message.Game?.SteamAppId == t.RemoteGame.Game.SteamAppId))
                .ToList();

            if (cachedItems.Any())
            {
                cachedItems.ForEach(UpdateCachedItem);

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }

        public void Handle(GameEditedEvent message)
        {
            var cachedItems = _cache.Values
                .Where(t =>
                    t.RemoteGame?.Game != null &&
                    (t.RemoteGame.Game.Id == message.Game?.Id ||
                     (message.Game?.IgdbId > 0 && t.RemoteGame.Game.IgdbId == message.Game?.IgdbId) ||
                     (message.Game?.SteamAppId > 0 && t.RemoteGame.Game.SteamAppId == message.Game?.SteamAppId)))
                .ToList();

            if (cachedItems.Any())
            {
                cachedItems.ForEach(UpdateCachedItem);

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }

        public void Handle(GamesBulkEditedEvent message)
        {
            var cachedItems = _cache.Values
                .Where(t =>
                    t.RemoteGame?.Game != null &&
                    message.Games.Any(m => m.Id == t.RemoteGame.Game.Id ||
                        (m.IgdbId > 0 && m.IgdbId == t.RemoteGame.Game.IgdbId) ||
                        (m.SteamAppId > 0 && m.SteamAppId == t.RemoteGame.Game.SteamAppId)))
                .ToList();

            if (cachedItems.Any())
            {
                cachedItems.ForEach(UpdateCachedItem);

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }

        public void Handle(GamesDeletedEvent message)
        {
            var cachedItems = _cache.Values
                .Where(t =>
                    t.RemoteGame?.Game != null &&
                    message.Games.Any(m => m.Id == t.RemoteGame.Game.Id ||
                        (m.IgdbId > 0 && m.IgdbId == t.RemoteGame.Game.IgdbId) ||
                        (m.SteamAppId > 0 && m.SteamAppId == t.RemoteGame.Game.SteamAppId)))
                .ToList();

            if (cachedItems.Any())
            {
                cachedItems.ForEach(UpdateCachedItem);

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }
    }
}
