using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class DlcOnlySpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public DlcOnlySpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.ParsedGameInfo == null)
            {
                return DownloadSpecDecision.Accept();
            }

            var contentType = subject.ParsedGameInfo.ContentType;

            // Reject DLC-only releases when searching for base game
            if (contentType == ReleaseContentType.DlcOnly)
            {
                _logger.Debug("Release is DLC-only, requires base game. Rejecting: {0}", subject.Release.Title);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.DlcOnly, "DLC-only release, requires base game");
            }

            // Reject update/patch-only releases
            if (contentType == ReleaseContentType.UpdateOnly)
            {
                _logger.Debug("Release is update/patch-only, requires base game. Rejecting: {0}", subject.Release.Title);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.UpdateOnly, "Update/patch-only release, requires base game");
            }

            // Reject season pass releases (DLC bundle without base game)
            if (contentType == ReleaseContentType.SeasonPass)
            {
                _logger.Debug("Release is season pass/DLC bundle only, requires base game. Rejecting: {0}", subject.Release.Title);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.SeasonPassOnly, "Season pass/DLC bundle, requires base game");
            }

            // Accept expansion packs (they may be standalone) and everything else
            return DownloadSpecDecision.Accept();
        }
    }
}
