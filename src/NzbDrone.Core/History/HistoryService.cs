using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryService
    {
        QualityModel GetBestQualityInHistory(QualityProfile profile, int gameId);
        PagingSpec<GameHistory> Paged(PagingSpec<GameHistory> pagingSpec, int[] languages, int[] qualities);
        GameHistory MostRecentForGame(int gameId);
        GameHistory MostRecentForDownloadId(string downloadId);
        GameHistory Get(int historyId);
        List<GameHistory> Find(string downloadId, GameHistoryEventType eventType);
        List<GameHistory> FindByDownloadId(string downloadId);
        List<GameHistory> GetByGameId(int gameId, GameHistoryEventType? eventType);
        void UpdateMany(List<GameHistory> toUpdate);
        string FindDownloadId(GameFileImportedEvent trackedDownload);
        List<GameHistory> Since(DateTime date, GameHistoryEventType? eventType);
    }

    public class HistoryService : IHistoryService,
                                  IHandle<GameGrabbedEvent>,
                                  IHandle<GameFileImportedEvent>,
                                  IHandle<DownloadFailedEvent>,
                                  IHandle<GameFileDeletedEvent>,
                                  IHandle<GameFileRenamedEvent>,
                                  IHandle<GamesDeletedEvent>,
                                  IHandle<DownloadIgnoredEvent>
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly Logger _logger;

        public HistoryService(IHistoryRepository historyRepository, Logger logger)
        {
            _historyRepository = historyRepository;
            _logger = logger;
        }

        public PagingSpec<GameHistory> Paged(PagingSpec<GameHistory> pagingSpec, int[] languages, int[] qualities)
        {
            return _historyRepository.GetPaged(pagingSpec, languages, qualities);
        }

        public GameHistory MostRecentForGame(int gameId)
        {
            return _historyRepository.MostRecentForGame(gameId);
        }

        public GameHistory MostRecentForDownloadId(string downloadId)
        {
            return _historyRepository.MostRecentForDownloadId(downloadId);
        }

        public GameHistory Get(int historyId)
        {
            return _historyRepository.Get(historyId);
        }

        public List<GameHistory> Find(string downloadId, GameHistoryEventType eventType)
        {
            return _historyRepository.FindByDownloadId(downloadId).Where(c => c.EventType == eventType).ToList();
        }

        public List<GameHistory> FindByDownloadId(string downloadId)
        {
            return _historyRepository.FindByDownloadId(downloadId);
        }

        public List<GameHistory> GetByGameId(int gameId, GameHistoryEventType? eventType)
        {
            return _historyRepository.GetByGameId(gameId, eventType);
        }

        public QualityModel GetBestQualityInHistory(QualityProfile profile, int gameId)
        {
            var comparer = new QualityModelComparer(profile);

            return _historyRepository.GetBestQualityInHistory(gameId).MaxBy(q => q, comparer);
        }

        public void UpdateMany(List<GameHistory> toUpdate)
        {
            _historyRepository.UpdateMany(toUpdate);
        }

        public string FindDownloadId(GameFileImportedEvent trackedDownload)
        {
            _logger.Debug("Trying to find downloadId for {0} from history", trackedDownload.ImportedGame.Path);

            var gameId = trackedDownload.GameInfo.Game.Id;
            var gameHistory = _historyRepository.FindDownloadHistory(gameId, trackedDownload.ImportedGame.Quality);

            var processedDownloadId = gameHistory
                .Where(c => c.EventType != GameHistoryEventType.Grabbed && c.DownloadId != null)
                .Select(c => c.DownloadId);

            var stillDownloading = gameHistory.Where(c => c.EventType == GameHistoryEventType.Grabbed && !processedDownloadId.Contains(c.DownloadId)).ToList();

            string downloadId = null;

            if (stillDownloading.Any())
            {
                if (stillDownloading.Count != 1)
                {
                    return null;
                }

                downloadId = stillDownloading.Single().DownloadId;
            }

            return downloadId;
        }

        public void Handle(GameGrabbedEvent message)
        {
            var history = new GameHistory
            {
                EventType = GameHistoryEventType.Grabbed,
                Date = DateTime.UtcNow,
                Quality = message.Game.ParsedGameInfo.Quality,
                Languages = message.Game.Languages,
                SourceTitle = message.Game.Release.Title,
                DownloadId = message.DownloadId,
                GameId = message.Game.Game.Id
            };

            history.Data.Add("Indexer", message.Game.Release.Indexer);
            history.Data.Add("NzbInfoUrl", message.Game.Release.InfoUrl);
            history.Data.Add("ReleaseGroup", message.Game.ParsedGameInfo.ReleaseGroup);
            history.Data.Add("Age", message.Game.Release.Age.ToString());
            history.Data.Add("AgeHours", message.Game.Release.AgeHours.ToString());
            history.Data.Add("AgeMinutes", message.Game.Release.AgeMinutes.ToString());
            history.Data.Add("PublishedDate", message.Game.Release.PublishDate.ToUniversalTime().ToString("s") + "Z");
            history.Data.Add("DownloadClient", message.DownloadClient);
            history.Data.Add("DownloadClientName", message.DownloadClientName);
            history.Data.Add("Size", message.Game.Release.Size.ToString());
            history.Data.Add("DownloadUrl", message.Game.Release.DownloadUrl);
            history.Data.Add("Guid", message.Game.Release.Guid);
            history.Data.Add("IgdbId", message.Game.Release.IgdbId.ToString());
            history.Data.Add("Protocol", ((int)message.Game.Release.DownloadProtocol).ToString());
            history.Data.Add("CustomFormatScore", message.Game.CustomFormatScore.ToString());
            history.Data.Add("GameMatchType", message.Game.GameMatchType.ToString());
            history.Data.Add("ReleaseSource", message.Game.ReleaseSource.ToString());
            history.Data.Add("IndexerFlags", message.Game.Release.IndexerFlags.ToString());
            history.Data.Add("IndexerId", message.Game.Release.IndexerId.ToString());

            if (!message.Game.ParsedGameInfo.ReleaseHash.IsNullOrWhiteSpace())
            {
                history.Data.Add("ReleaseHash", message.Game.ParsedGameInfo.ReleaseHash);
            }

            if (message.Game.Release is TorrentInfo torrentRelease)
            {
                history.Data.Add("TorrentInfoHash", torrentRelease.InfoHash);
            }

            _historyRepository.Insert(history);
        }

        public void Handle(GameFileImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadId = message.DownloadId;

            if (downloadId.IsNullOrWhiteSpace())
            {
                downloadId = FindDownloadId(message); // For now fuck off.
            }

            var game = message.GameInfo.Game;
            var history = new GameHistory
            {
                EventType = GameHistoryEventType.DownloadFolderImported,
                Date = DateTime.UtcNow,
                Quality = message.GameInfo.Quality,
                Languages = message.GameInfo.Languages,
                SourceTitle = message.ImportedGame.SceneName ?? Path.GetFileNameWithoutExtension(message.GameInfo.Path),
                DownloadId = downloadId,
                GameId = game.Id,
            };

            history.Data.Add("FileId", message.ImportedGame.Id.ToString());
            history.Data.Add("DroppedPath", message.GameInfo.Path);
            history.Data.Add("ImportedPath", Path.Combine(game.Path, message.ImportedGame.RelativePath));
            history.Data.Add("DownloadClient", message.DownloadClientInfo?.Type);
            history.Data.Add("DownloadClientName", message.DownloadClientInfo?.Name);
            history.Data.Add("ReleaseGroup", message.GameInfo.ReleaseGroup);
            history.Data.Add("CustomFormatScore", message.GameInfo.CustomFormatScore.ToString());
            history.Data.Add("Size", message.GameInfo.Size.ToString());
            history.Data.Add("IndexerFlags", message.ImportedGame.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(GameFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.NoLinkedEpisodes)
            {
                _logger.Debug("Removing game file from DB as part of cleanup routine, not creating history event.");
                return;
            }

            var history = new GameHistory
            {
                EventType = GameHistoryEventType.GameFileDeleted,
                Date = DateTime.UtcNow,
                Quality = message.GameFile.Quality,
                Languages = message.GameFile.Languages,
                SourceTitle = message.GameFile.Path,
                GameId = message.GameFile.GameId
            };

            history.Data.Add("Reason", message.Reason.ToString());
            history.Data.Add("ReleaseGroup", message.GameFile.ReleaseGroup);
            history.Data.Add("Size", message.GameFile.Size.ToString());
            history.Data.Add("IndexerFlags", message.GameFile.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(GameFileRenamedEvent message)
        {
            var sourcePath = message.OriginalPath;
            var sourceRelativePath = message.Game.Path.GetRelativePath(message.OriginalPath);
            var path = Path.Combine(message.Game.Path, message.GameFile.RelativePath);
            var relativePath = message.GameFile.RelativePath;

            var history = new GameHistory
            {
                EventType = GameHistoryEventType.GameFileRenamed,
                Date = DateTime.UtcNow,
                Quality = message.GameFile.Quality,
                Languages = message.GameFile.Languages,
                SourceTitle = message.OriginalPath,
                GameId = message.GameFile.GameId,
            };

            history.Data.Add("SourcePath", sourcePath);
            history.Data.Add("SourceRelativePath", sourceRelativePath);
            history.Data.Add("Path", path);
            history.Data.Add("RelativePath", relativePath);
            history.Data.Add("ReleaseGroup", message.GameFile.ReleaseGroup);
            history.Data.Add("Size", message.GameFile.Size.ToString());
            history.Data.Add("IndexerFlags", message.GameFile.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(DownloadIgnoredEvent message)
        {
            var history = new GameHistory
            {
                EventType = GameHistoryEventType.DownloadIgnored,
                Date = DateTime.UtcNow,
                Quality = message.Quality,
                SourceTitle = message.SourceTitle,
                GameId = message.GameId,
                DownloadId = message.DownloadId,
                Languages = message.Languages
            };

            history.Data.Add("DownloadClient", message.DownloadClientInfo.Type);
            history.Data.Add("DownloadClientName", message.DownloadClientInfo.Name);
            history.Data.Add("Message", message.Message);
            history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteGame?.ParsedGameInfo?.ReleaseGroup);
            history.Data.Add("Size", message.TrackedDownload?.DownloadItem.TotalSize.ToString());
            history.Data.Add("Indexer", message.TrackedDownload?.RemoteGame?.Release?.Indexer);

            _historyRepository.Insert(history);
        }

        public void Handle(GamesDeletedEvent message)
        {
            _historyRepository.DeleteForGames(message.Games.Select(m => m.Id).ToList());
        }

        public void Handle(DownloadFailedEvent message)
        {
            var history = new GameHistory
            {
                EventType = GameHistoryEventType.DownloadFailed,
                Date = DateTime.UtcNow,
                Quality = message.Quality,
                Languages = message.Languages,
                SourceTitle = message.SourceTitle,
                GameId = message.GameId,
                DownloadId = message.DownloadId
            };

            history.Data.Add("DownloadClient", message.DownloadClient);
            history.Data.Add("DownloadClientName", message.TrackedDownload?.DownloadItem.DownloadClientInfo.Name);
            history.Data.Add("Message", message.Message);
            history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteGame?.ParsedGameInfo?.ReleaseGroup ?? message.Data.GetValueOrDefault(GameHistory.RELEASE_GROUP));
            history.Data.Add("Size", message.TrackedDownload?.DownloadItem.TotalSize.ToString() ?? message.Data.GetValueOrDefault(GameHistory.SIZE));
            history.Data.Add("Indexer", message.TrackedDownload?.RemoteGame?.Release?.Indexer ?? message.Data.GetValueOrDefault(GameHistory.INDEXER));

            _historyRepository.Insert(history);
        }

        public List<GameHistory> Since(DateTime date, GameHistoryEventType? eventType)
        {
            return _historyRepository.Since(date, eventType);
        }
    }
}
