using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class AvailabilitySpecification : IDownloadDecisionEngineSpecification
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public AvailabilitySpecification(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria is { UserInvokedSearch: true })
            {
                _logger.Debug("Skipping availability check during search");
                return DownloadSpecDecision.Accept();
            }

            var availabilityDelay = _configService.AvailabilityDelay;

            if (!subject.Game.IsAvailable(availabilityDelay))
            {
                return DownloadSpecDecision.Reject(DownloadRejectionReason.Availability, "Game {0} will only be considered available {1} days after {2}", subject.Game, availabilityDelay, subject.Game.MinimumAvailability.ToString());
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
