using System.IO;
using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.Archives;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    /// <summary>
    /// Backstop for the "never import an archive verbatim" guarantee.
    ///
    /// .zip/.rar/.7z stay in <see cref="MediaFileExtensions"/> on purpose —
    /// removing them would stop the parser stripping ".7z" from release titles
    /// and would make archive-wrapped releases invisible in the manual import
    /// modal. So instead: an archive that reached this point without a successful
    /// inspection (unsupported container, split volume set, zero or several game
    /// files inside) is rejected rather than copied across as-is, where no
    /// library scanner could read it.
    /// </summary>
    public class NotArchiveSpecification : IImportDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public NotArchiveSpecification(Logger logger)
        {
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            if (localGame.ArchiveInspection != null)
            {
                return ImportSpecDecision.Accept();
            }

            if (!GameArchiveService.IsContainer(localGame.Path))
            {
                return ImportSpecDecision.Accept();
            }

            var extension = Path.GetExtension(localGame.Path);

            _logger.Debug("Rejected archive with no extractable game file: {0}", localGame.Path);

            return ImportSpecDecision.Reject(ImportRejectionReason.ArchiveFile,
                $"Archive '{extension}' does not contain a single extractable game file, it needs to be extracted manually");
        }
    }
}
