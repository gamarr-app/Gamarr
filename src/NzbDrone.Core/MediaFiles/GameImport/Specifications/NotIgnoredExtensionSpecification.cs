using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    /// <summary>
    /// Rejects a single-file import whose extension is on the user's ignored
    /// list. Release folders are not judged by their own name: the ignored files
    /// inside are left out of the transfer instead.
    /// </summary>
    public class NotIgnoredExtensionSpecification : IImportDecisionEngineSpecification
    {
        private readonly IIgnoredExtensionService _ignoredExtensionService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public NotIgnoredExtensionSpecification(IIgnoredExtensionService ignoredExtensionService, IDiskProvider diskProvider, Logger logger)
        {
            _ignoredExtensionService = ignoredExtensionService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var path = localGame?.Path;

            if (!_ignoredExtensionService.IsIgnored(path) || _diskProvider.FolderExists(path))
            {
                return ImportSpecDecision.Accept();
            }

            var extension = Path.GetExtension(path);

            _logger.Debug("[{0}] has an ignored extension: '{1}'", path, extension);

            return ImportSpecDecision.Reject(ImportRejectionReason.IgnoredExtension,
                $"Extension '{extension}' is in the ignored file extensions list");
        }
    }
}
