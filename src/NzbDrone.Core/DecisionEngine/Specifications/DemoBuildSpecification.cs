using NLog;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    /// <summary>
    /// Rejects kiosk/store-demo, trial and preview builds (#207).
    /// </summary>
    /// <remarks>
    /// These parse and match like any other release - "Game.KIOSK.eShop.
    /// INTERNAL.NSW-VENOM" was approved, grabbed and imported as the base game -
    /// but a demo is a cut-down build, not the game. It is never an acceptable
    /// base game and never an upgrade, so the rejection is permanent and
    /// unconditional; a user who does want one can still grab it manually.
    /// </remarks>
    public class DemoBuildSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public DemoBuildSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, ReleaseDecisionInformation information)
        {
            if (subject.ParsedGameInfo?.ContentType != ReleaseContentType.Demo)
            {
                return DownloadSpecDecision.Accept();
            }

            _logger.Debug("Release is a demo/kiosk build, rejecting: {0}", CleanseLogMessage.SanitizeLogParam(subject.Release?.Title));

            return DownloadSpecDecision.Reject(DownloadRejectionReason.DemoBuild, "Demo, trial or kiosk build, not the full game");
        }
    }
}
