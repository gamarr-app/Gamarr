using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    public class MatchesGrabSpecification : IImportDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MatchesGrabSpecification(Logger logger)
        {
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            if (localGame.ExistingFile)
            {
                return ImportSpecDecision.Accept();
            }

            var releaseInfo = localGame.Release;

            if (releaseInfo == null || releaseInfo.GameIds.Empty())
            {
                return ImportSpecDecision.Accept();
            }

            if (releaseInfo.GameIds.All(o => o != localGame.Game.Id))
            {
                _logger.Debug("Unexpected game(s) in file: {0}", localGame.Game.ToString());

                return ImportSpecDecision.Reject(ImportRejectionReason.GameNotFoundInRelease, "Game {0} was not found in the grabbed release: {1}", localGame.Game.ToString(), releaseInfo.Title);
            }

            return ImportSpecDecision.Accept();
        }
    }
}
