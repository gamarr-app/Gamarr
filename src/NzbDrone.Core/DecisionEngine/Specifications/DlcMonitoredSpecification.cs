using System.Linq;
using NLog;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    // DLC slots are opt-in (created monitored=false from metadata), so nothing
    // automatic may grab a DLC nobody asked for. Only an *interactive* search
    // bypasses this: there the user is looking at one named release and picks
    // it, so the slot flag is not what they are expressing an opinion with.
    //
    // UserInvokedSearch is deliberately NOT the gate (#214). It is also true for
    // a game search, "Search All", a missing search and a cutoff-unmet search -
    // bulk operations that name a *game*, not a DLC - and gating on it let those
    // grab DLC for unmonitored slots. Same reasoning as
    // UpdateMonitoredSpecification: the game-level flag may be bypassed during a
    // search because the search names that game, but a flag that singles out a
    // kind of content inside the game may not.
    //
    // The interactive bypass only controls how the release is *shown* in the
    // release list; the grab itself (ReleaseController.DownloadRelease) hands the
    // report straight to the download service and runs no specifications, so a
    // deliberate interactive grab of an unmonitored DLC works either way.
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

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, ReleaseDecisionInformation information)
        {
            if (information.SearchCriteria is { InteractiveSearch: true })
            {
                _logger.Debug("Skipping DLC monitored check during interactive search");
                return DownloadSpecDecision.Accept();
            }

            var contentType = subject.ParsedGameInfo?.ContentType ?? ReleaseContentType.Unknown;

            // Deliberately narrower than ReleaseContentTypeExtensions.OccupiesDlcSlot(),
            // which also covers Expansion. An expansion may be standalone and
            // may be its own library entry, and such a game has no DLC slots at
            // all - gating it here would reject it outright on RSS. A named
            // expansion that really is an add-on carries an explicit DLC marker
            // in practice and classifies as DlcOnly, so it is gated below (#212).
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

            var gameTitle = subject.Game.Title;

            var matching = dlcSlots.FirstOrDefault(c =>
                GameComponentMatcher.ReleaseMatchesDlcTitle(subject.Release.Title, c.Title, gameTitle) ||
                GameComponentMatcher.ReleaseMatchesDlcTitle(subject.ParsedGameInfo.PrimaryGameTitle, c.Title, gameTitle));

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
