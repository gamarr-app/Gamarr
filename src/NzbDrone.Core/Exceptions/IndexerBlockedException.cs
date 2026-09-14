using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Exceptions
{
    // Thrown instead of attempting a grab against an indexer that is currently backed off after
    // recent failures. It derives from ReleaseDownloadException so the existing grab call sites
    // keep treating it as "this grab did not happen", but it is distinguishable because nothing
    // was actually sent: no request was made, so nothing new should be recorded against the
    // indexer for it.
    public class IndexerBlockedException : ReleaseDownloadException
    {
        public IndexerBlockedException(ReleaseInfo release, string message)
            : base(release, message)
        {
        }

        public IndexerBlockedException(ReleaseInfo release, string message, params object[] args)
            : base(release, message, args)
        {
        }
    }
}
