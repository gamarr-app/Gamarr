using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    // A regional edition of a console game is its own build with its own title
    // ID, so an update or DLC built for one cannot be applied to another: a USA
    // update will not install over an EUR base. The region tags were parsed only
    // to be stripped off titles, and nothing compared an add-on's region with
    // the base game's, so the EUR and USA update lines for the same game ranked
    // equally and the pick between them was arbitrary (#213).
    //
    // This rejects an add-on whose explicit region tag differs from the explicit
    // region tag on the installed base file's scene name. The bar is
    // deliberately high on both sides:
    //
    // - A release with NO region tag is always accepted. Most titles ship as a
    //   single worldwide edition and are posted without a region, so rejecting
    //   region-less releases would break the common case to fix the rare one.
    // - A base file with NO region tag accepts everything. There is nothing to
    //   compare against, and guessing from metadata would be a false reject.
    // - Overlapping sets are accepted: a multi-region dump that lists the base's
    //   region among others is applicable.
    //
    // Only the add-on content types are gated. A base-game release is not an
    // add-on to anything, and a user switching editions should not be blocked
    // from grabbing the new base game by the edition they already have.
    public class RegionMatchSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public RegionMatchSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, ReleaseDecisionInformation information)
        {
            var contentType = subject.ParsedGameInfo?.ContentType ?? ReleaseContentType.Unknown;

            // Updates, DLC, season passes and expansions all attach to a base
            // build; anything that carries its own base game does not.
            if (!contentType.RequiresBaseGame())
            {
                return DownloadSpecDecision.Accept();
            }

            var baseFile = subject.Game?.GameFile;

            if (baseFile == null)
            {
                return DownloadSpecDecision.Accept();
            }

            var baseRegions = ConsoleTitleParser.ParseRegions(baseFile.GetSceneOrFileName());

            if (baseRegions.Count == 0)
            {
                return DownloadSpecDecision.Accept();
            }

            var releaseRegions = ConsoleTitleParser.ParseRegions(subject.Release?.Title);

            if (releaseRegions.Count == 0)
            {
                return DownloadSpecDecision.Accept();
            }

            if (releaseRegions.Overlaps(baseRegions))
            {
                return DownloadSpecDecision.Accept();
            }

            _logger.Debug("Release region [{0}] does not match the installed base game region [{1}], rejecting",
                Describe(releaseRegions),
                Describe(baseRegions));

            return DownloadSpecDecision.Reject(DownloadRejectionReason.RegionMismatch,
                "Edition mismatch: base {0}, release {1}",
                Describe(baseRegions),
                Describe(releaseRegions));
        }

        private static string Describe(IEnumerable<string> regions)
        {
            return string.Join("/", regions.OrderBy(r => r));
        }
    }
}
