using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class AcceptableSizeSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public AcceptableSizeSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            // Size validation based on runtime doesn't apply to games.
            // Game file sizes have no correlation with playtime/runtime.
            // A 2-hour indie game might be 500MB while a 2-hour AAA game could be 100GB.
            _logger.Debug("Skipping size check for games - runtime-based size validation not applicable");
            return DownloadSpecDecision.Accept();
        }
    }
}
