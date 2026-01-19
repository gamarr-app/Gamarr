using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class QualityAllowedByProfileSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public QualityAllowedByProfileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            _logger.Debug("Checking if report meets quality requirements. {0}", subject.ParsedGameInfo.Quality);

            var profile = subject.Game.QualityProfile;
            var qualityIndex = profile.GetIndex(subject.ParsedGameInfo.Quality.Quality);
            var qualityOrGroup = profile.Items[qualityIndex.Index];

            if (!qualityOrGroup.Allowed)
            {
                _logger.Debug("Quality {0} rejected by Game's quality profile", subject.ParsedGameInfo.Quality);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.QualityNotWanted, "{0} is not wanted in profile", subject.ParsedGameInfo.Quality.Quality);
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
