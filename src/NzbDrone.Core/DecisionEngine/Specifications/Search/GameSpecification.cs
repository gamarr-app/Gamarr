using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class GameSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public GameSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return DownloadSpecDecision.Accept();
            }

            _logger.Debug("Checking if game matches searched game");

            if (subject.Game.Id != searchCriteria.Game.Id)
            {
                _logger.Debug("Game {0} does not match {1}", subject.Game, searchCriteria.Game);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.WrongGame, "Wrong game");
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
