using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    public class AlreadyImportedSpecification : IImportDecisionEngineSpecification
    {
        private readonly IHistoryService _historyService;
        private readonly Logger _logger;

        public AlreadyImportedSpecification(IHistoryService historyService,
                                            Logger logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Database;

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            if (downloadClientItem == null)
            {
                _logger.Debug("No download client information is available, skipping");
                return ImportSpecDecision.Accept();
            }

            var game = localGame.Game;

            if (!game.HasFile)
            {
                _logger.Debug("Skipping already imported check for game without file");
                return ImportSpecDecision.Accept();
            }

            var gameImportedHistory = _historyService.GetByGameId(game.Id, null);
            var lastImported = gameImportedHistory.FirstOrDefault(h =>
                h.DownloadId == downloadClientItem.DownloadId &&
                h.EventType == GameHistoryEventType.DownloadFolderImported);
            var lastGrabbed = gameImportedHistory.FirstOrDefault(h =>
                h.DownloadId == downloadClientItem.DownloadId && h.EventType == GameHistoryEventType.Grabbed);

            if (lastImported == null)
            {
                _logger.Trace("Game file has not been imported");
                return ImportSpecDecision.Accept();
            }

            if (lastGrabbed != null)
            {
                // If the release was grabbed again after importing don't reject it
                if (lastGrabbed.Date.After(lastImported.Date))
                {
                    _logger.Trace("Game file was grabbed again after importing");
                    return ImportSpecDecision.Accept();
                }

                // If the release was imported after the last grab reject it
                if (lastImported.Date.After(lastGrabbed.Date))
                {
                    _logger.Debug("Game file previously imported at {0}", lastImported.Date);
                    return ImportSpecDecision.Reject(ImportRejectionReason.GameAlreadyImported, "Game file already imported at {0}", lastImported.Date.ToLocalTime());
                }
            }
            else
            {
                _logger.Debug("Game file previously imported at {0}", lastImported.Date);
                return ImportSpecDecision.Reject(ImportRejectionReason.GameAlreadyImported, "Game file already imported at {0}", lastImported.Date.ToLocalTime());
            }

            return ImportSpecDecision.Accept();
        }
    }
}
