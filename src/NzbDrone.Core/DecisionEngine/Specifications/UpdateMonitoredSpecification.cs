using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    // The update-slot analog of DlcMonitoredSpecification. Nothing used to
    // consult either monitoring flag for an update release, so an update was
    // grabbed even with game.MonitorUpdates off and the update component
    // unmonitored (#203).
    //
    // Unlike MonitoredGameSpecification this is NOT bypassed for a user-invoked
    // search: asking Gamarr to search a game is not a request for its updates
    // when the user has explicitly turned updates off. The game-level flag is
    // bypassed during a search because the search names that game; these flags
    // single out a kind of content within it, the way the DLC flags do.
    public class UpdateMonitoredSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly IGameComponentService _componentService;
        private readonly Logger _logger;

        public UpdateMonitoredSpecification(IGameComponentService componentService, Logger logger)
        {
            _componentService = componentService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, ReleaseDecisionInformation information)
        {
            var contentType = subject.ParsedGameInfo?.ContentType ?? ReleaseContentType.Unknown;

            if (contentType != ReleaseContentType.UpdateOnly)
            {
                return DownloadSpecDecision.Accept();
            }

            if (subject.Game == null)
            {
                return DownloadSpecDecision.Accept();
            }

            if (!subject.Game.MonitorUpdates)
            {
                _logger.Debug("Updates are not monitored for this game, rejecting update release");

                return DownloadSpecDecision.Reject(DownloadRejectionReason.UpdateNotMonitored, "Updates are not monitored for this game");
            }

            var updateSlots = (_componentService.GetByGame(subject.Game.Id) ?? new List<GameComponent>())
                              .Where(c => c.ComponentType == GameComponentType.Update)
                              .ToList();

            // Update components only exist once an update has been imported, so
            // having none means nothing has been opted out of yet.
            if (!updateSlots.Any())
            {
                return DownloadSpecDecision.Accept();
            }

            var version = subject.ParsedGameInfo.GameVersion;

            if (version?.HasValue == true)
            {
                var matching = updateSlots.FirstOrDefault(c => GameVersion.Parse(c.Key).Equals(version));

                if (matching != null && !matching.Monitored)
                {
                    return DownloadSpecDecision.Reject(DownloadRejectionReason.UpdateNotMonitored, "Update '{0}' is not monitored", matching.Title);
                }
            }

            if (!updateSlots.Any(c => c.Monitored))
            {
                return DownloadSpecDecision.Reject(DownloadRejectionReason.UpdateNotMonitored, "No update slot for this game is monitored");
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
