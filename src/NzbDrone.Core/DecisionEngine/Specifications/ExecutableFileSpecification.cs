using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class ExecutableFileSpecification : IDownloadDecisionEngineSpecification
    {
        // Trackers and repackers routinely sign a title with their own address
        // ("Game.Title.REPACK-FitGirl www.example.com"), which reads as a
        // ".com" executable. Skip those instead of rejecting them.
        private static readonly Regex TrailingUrlRegex = new Regex(
            @"(?:https?://|www\.)[^\s\]\)]*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Logger _logger;

        public ExecutableFileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            var title = subject.Release?.Title;

            if (title.IsNullOrWhiteSpace())
            {
                return DownloadSpecDecision.Accept();
            }

            title = title.Trim();

            if (TrailingUrlRegex.IsMatch(title))
            {
                return DownloadSpecDecision.Accept();
            }

            // Only the LAST extension matters: "Show.S01E01.1080p.mkv.exe" is a
            // Windows binary, not a video, and the same trick works on any
            // content extension a games indexer might carry.
            if (!FileExtensions.IsUnsafeExecutable(title))
            {
                return DownloadSpecDecision.Accept();
            }

            var extension = FileExtensions.GetEffectiveExtension(title);

            _logger.Debug("Release ends in executable extension {0}, rejecting: {1}",
                extension,
                CleanseLogMessage.SanitizeLogParam(title));

            return DownloadSpecDecision.Reject(
                DownloadRejectionReason.ExecutableFile,
                "Release is an executable file ({0}), not a game release",
                extension);
        }
    }
}
