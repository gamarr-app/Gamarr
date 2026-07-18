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

        // Console platforms that should be rejected by default for PC games
        private static readonly PlatformFamily[] ConsolePlatforms =
        {
            PlatformFamily.PlayStation,
            PlatformFamily.Xbox,
            PlatformFamily.Nintendo,
            PlatformFamily.Sega,
            PlatformFamily.Atari,
            PlatformFamily.Mobile
        };

        public PlatformSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            var preferredPlatforms = subject.EffectiveQualityProfile.PreferredPlatforms;
            var releasePlatform = subject.ParsedGameInfo.Platform;

            // Per-entry platform (#150) takes precedence over profile
            // preferences: a PS4 entry only accepts PS4-tagged releases, etc.
            // PC-family entries accept untagged releases (scene PC releases
            // rarely carry a platform token); console entries require an
            // explicit tag since console releases always declare one.
            var gamePlatform = subject.Game.Platform;

            if (gamePlatform != PlatformFamily.Unknown)
            {
                if (releasePlatform == gamePlatform)
                {
                    return DownloadSpecDecision.Accept();
                }

                var isPcFamily = gamePlatform is PlatformFamily.PC or PlatformFamily.Linux or PlatformFamily.Mac;

                if (releasePlatform == PlatformFamily.Unknown && isPcFamily)
                {
                    return DownloadSpecDecision.Accept();
                }

                _logger.Debug("Release platform {0} does not match this entry's platform {1}, rejecting.", releasePlatform, gamePlatform);

                return DownloadSpecDecision.Reject(
                    DownloadRejectionReason.WantedPlatform,
                    "Release platform {0} does not match this entry's platform {1}",
                    releasePlatform,
                    gamePlatform);
            }

            // If platform preferences are set, use them
            if (preferredPlatforms != null && preferredPlatforms.Any())
            {
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

            // No platform preferences set - reject known console platforms by default
            if (ConsolePlatforms.Contains(releasePlatform))
            {
                _logger.Debug("Release platform {0} is a console platform, rejecting (no platform preferences set, defaulting to PC).",
                    releasePlatform);

                return DownloadSpecDecision.Reject(
                    DownloadRejectionReason.WantedPlatform,
                    "Release platform {0} is a console platform (PS/Xbox/Nintendo/etc), not compatible with PC",
                    releasePlatform);
            }

            _logger.Debug("Release platform {0} is acceptable (PC-compatible or unknown).", releasePlatform);
            return DownloadSpecDecision.Accept();
        }
    }
}
