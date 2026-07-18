using System.Linq;
using NLog;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    // DLC slots are opt-in (created monitored=false from metadata), so RSS and
    // scheduled searches must not grab a DLC nobody asked for. User-invoked
    // searches bypass this, matching how MonitoredGameSpecification treats the
    // game-level flag.
    public class DlcMonitoredSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly IGameComponentService _componentService;
        private readonly Logger _logger;

        public DlcMonitoredSpecification(IGameComponentService componentService, Logger logger)
        {
            _componentService = componentService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null && searchCriteria.UserInvokedSearch)
            {
                _logger.Debug("Skipping DLC monitored check during search");
                return DownloadSpecDecision.Accept();
            }

            var contentType = subject.ParsedGameInfo?.ContentType ?? ReleaseContentType.Unknown;

            if (contentType != ReleaseContentType.DlcOnly && contentType != ReleaseContentType.SeasonPass)
            {
                return DownloadSpecDecision.Accept();
            }

            if (subject.Game == null)
            {
                return DownloadSpecDecision.Accept();
            }

            var dlcSlots = _componentService.GetByGame(subject.Game.Id)
                                            .Where(c => c.ComponentType == GameComponentType.Dlc)
                                            .ToList();

            var matching = dlcSlots.FirstOrDefault(c =>
                GameComponentMatcher.ReleaseMatchesDlcTitle(subject.Release.Title, c.Title) ||
                GameComponentMatcher.ReleaseMatchesDlcTitle(subject.ParsedGameInfo.PrimaryGameTitle, c.Title));

            if (matching == null)
            {
                return DownloadSpecDecision.Reject(DownloadRejectionReason.DlcNotMonitored, "No monitored DLC slot matches this release");
            }

            if (!matching.Monitored)
            {
                return DownloadSpecDecision.Reject(DownloadRejectionReason.DlcNotMonitored, "DLC '{0}' is not monitored", matching.Title);
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
