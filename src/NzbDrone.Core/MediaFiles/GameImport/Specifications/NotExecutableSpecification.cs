using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    public class NotExecutableSpecification : IImportDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public NotExecutableSpecification(Logger logger)
        {
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var path = localGame?.Path;

            if (path.IsNullOrWhiteSpace() || !FileExtensions.IsUnsafeExecutable(path))
            {
                return ImportSpecDecision.Accept();
            }

            var extension = FileExtensions.GetEffectiveExtension(path);

            // A content extension in front of an executable one is never
            // innocent — "Show.S01E01.mkv.exe" is a Windows binary. This is
            // checked first because .exe/.msi are otherwise allowed below.
            if (FileExtensions.IsMasqueradedExecutable(path))
            {
                _logger.Debug("[{0}] hides an executable behind a content extension", path);

                return ImportSpecDecision.Reject(
                    ImportRejectionReason.ExecutableFile,
                    "Caution: Found executable disguised as a content file: '{0}'",
                    extension);
            }

            // .exe and .msi are legitimate game payloads (repack and GOG
            // installers ship them, see MediaFileExtensions), so they survive
            // the check above. Everything else on the unsafe list — .scr, .com,
            // .js, .jar, .bat, .ps1 and friends — is never a game file.
            if (!MediaFileExtensions.IsGameFileExtension(extension))
            {
                _logger.Debug("[{0}] is an executable, not a game file", path);

                return ImportSpecDecision.Reject(
                    ImportRejectionReason.ExecutableFile,
                    "Caution: Found executable file with extension: '{0}'",
                    extension);
            }

            return ImportSpecDecision.Accept();
        }
    }
}
