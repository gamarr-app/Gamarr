using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class MonitoredGameSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MonitoredGameSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                if (searchCriteria.UserInvokedSearch)
                {
                    _logger.Debug("Skipping monitored check during search");
                    return DownloadSpecDecision.Accept();
                }
            }

            if (!subject.Game.Monitored)
            {
                _logger.Debug("{0} is present in the DB but not tracked. Rejecting", subject.Game);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.GameNotMonitored, "Game is not monitored");
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
