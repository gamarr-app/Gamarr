using System.Linq;
using NLog;
using NzbDrone.Core.Games;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class PlatformSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public PlatformSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            var preferredPlatforms = subject.Game.QualityProfile.PreferredPlatforms;

            if (preferredPlatforms == null || !preferredPlatforms.Any())
            {
                _logger.Debug("Profile has no platform restrictions, accepting release.");
                return DownloadSpecDecision.Accept();
            }

            var releasePlatform = subject.ParsedGameInfo.Platform;

            if (releasePlatform == PlatformFamily.Unknown)
            {
                _logger.Debug("Release platform is unknown, accepting release.");
                return DownloadSpecDecision.Accept();
            }

            if (preferredPlatforms.Contains(releasePlatform))
            {
                _logger.Debug("Release platform {0} matches preferred platforms, accepting.", releasePlatform);
                return DownloadSpecDecision.Accept();
            }

            _logger.Debug("Release platform {0} is not in preferred platforms [{1}], rejecting.",
                releasePlatform,
                string.Join(", ", preferredPlatforms));

            return DownloadSpecDecision.Reject(
                DownloadRejectionReason.WantedPlatform,
                "Release platform {0} is not wanted, wanted [{1}]",
                releasePlatform,
                string.Join(", ", preferredPlatforms));
        }
    }
}
