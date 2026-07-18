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

            if (!contentType.RequiresBaseGame())
            {
                // Expansion packs (may be standalone) and full releases
                return DownloadSpecDecision.Accept();
            }

            // Updates, DLC and season passes are importable ALONGSIDE an
            // existing base game (#149 phase 0) — only reject when the game
            // has no base on disk to apply them to.
            if (subject.Game?.HasFile == true)
            {
                _logger.Debug("Release requires the base game and one is on disk, accepting: {0}", subject.Release.Title);
                return DownloadSpecDecision.Accept();
            }

            return contentType switch
            {
                ReleaseContentType.DlcOnly => DownloadSpecDecision.Reject(DownloadRejectionReason.DlcOnly, "DLC-only release, requires base game (none on disk)"),
                ReleaseContentType.SeasonPass => DownloadSpecDecision.Reject(DownloadRejectionReason.SeasonPassOnly, "Season pass/DLC bundle, requires base game (none on disk)"),
                _ => DownloadSpecDecision.Reject(DownloadRejectionReason.UpdateOnly, "Update/patch-only release, requires base game (none on disk)")
            };
        }
    }
}
