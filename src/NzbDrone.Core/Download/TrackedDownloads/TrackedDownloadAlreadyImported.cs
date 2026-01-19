using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.History;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadAlreadyImported
    {
        bool IsImported(TrackedDownload trackedDownload, List<GameHistory> historyItems);
    }

    public class TrackedDownloadAlreadyImported : ITrackedDownloadAlreadyImported
    {
        private readonly Logger _logger;

        public TrackedDownloadAlreadyImported(Logger logger)
        {
            _logger = logger;
        }

        public bool IsImported(TrackedDownload trackedDownload, List<GameHistory> historyItems)
        {
            _logger.Trace("Checking if all games for '{0}' have been imported", trackedDownload.DownloadItem.Title);

            if (historyItems.Empty())
            {
                _logger.Trace("No history for {0}", trackedDownload.DownloadItem.Title);
                return false;
            }

            var game = trackedDownload.RemoteGame.Game;

            var lastHistoryItem = historyItems.FirstOrDefault(h => h.GameId == game.Id);

            if (lastHistoryItem == null)
            {
                _logger.Trace("No history for game: {0}", game.ToString());
                return false;
            }

            var allGamesImportedInHistory = lastHistoryItem.EventType == GameHistoryEventType.DownloadFolderImported;
            _logger.Trace("Last event for game: {0} is: {1}", game, lastHistoryItem.EventType);

            _logger.Trace("All games for '{0}' have been imported: {1}", trackedDownload.DownloadItem.Title, allGamesImportedInHistory);
            return allGamesImportedInHistory;
        }
    }
}
