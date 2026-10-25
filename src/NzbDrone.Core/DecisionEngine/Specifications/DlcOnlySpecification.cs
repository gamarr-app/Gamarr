using NLog;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class DlcOnlySpecification : IDownloadDecisionEngineSpecification
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public DlcOnlySpecification(IMediaFileService mediaFileService, Logger logger)
        {
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, ReleaseDecisionInformation information)
        {
            if (subject.ParsedGameInfo == null)
            {
                return DownloadSpecDecision.Accept();
            }

            var contentType = subject.ParsedGameInfo.ContentType;

            if (!contentType.RequiresBaseGame())
            {
                // Full releases, complete editions and demos
                return DownloadSpecDecision.Accept();
            }

            // An expansion became base-requiring in #212 so that the prefix
            // match in ParsingService can find its base game, but it is still
            // the one content type that MAY be standalone - a standalone
            // expansion is usually its own library entry, and that game has no
            // base file on disk precisely because this is the release that would
            // provide it. Gating it on one would reject it, so this keeps the
            // accept it had before #212; the slot-level checks downstream are
            // where an expansion that really is an add-on gets judged.
            if (contentType == ReleaseContentType.Expansion)
            {
                return DownloadSpecDecision.Accept();
            }

            // Updates, DLC and season passes are importable ALONGSIDE an
            // existing base game (#149 phase 0) — only reject when the game
            // has no base on disk to apply them to.
            if (subject.Game?.HasFile == true)
            {
                _logger.Debug("Release requires the base game and one is on disk, accepting: {0}", CleanseLogMessage.SanitizeLogParam(subject.Release.Title));
                return DownloadSpecDecision.Accept();
            }

            // HasFile is only the game's primary-file pointer, and the pointer
            // is not the same question as "is a base game on disk". The
            // post-import update search (#210) runs while the base file row
            // already exists but the pointer has not been written yet, so every
            // update in it was rejected as "none on disk" and a manual search
            // minutes later took the same release (#216). The pointer can also
            // legitimately name a unit other than the base. So before rejecting,
            // ask the files.
            if (subject.Game?.Id > 0 && GameFileUnit.HasBaseOnDisk(_mediaFileService.GetFilesByGame(subject.Game.Id)))
            {
                _logger.Debug("Release requires the base game and a base file is recorded for the game, accepting: {0}", CleanseLogMessage.SanitizeLogParam(subject.Release.Title));
                return DownloadSpecDecision.Accept();
            }

            return contentType switch
            {
                ReleaseContentType.DlcOnly => DownloadSpecDecision.Reject(DownloadRejectionReason.DlcOnly, "DLC-only release, requires base game (none on disk)"),
                ReleaseContentType.SeasonPass => DownloadSpecDecision.Reject(DownloadRejectionReason.SeasonPassOnly, "Season pass/DLC bundle, requires base game (none on disk)"),
                _ => DownloadSpecDecision.Reject(DownloadRejectionReason.UpdateOnly, "Update/patch-only release, requires base game (none on disk)")
            };
        }
    }
}
