using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    /// <summary>
    /// This specification validates that files match their containing folder.
    /// For games, folder structures vary widely (unlike movies), so this
    /// specification accepts all files by design.
    /// </summary>
    public class MatchesFolderSpecification : IImportDecisionEngineSpecification
    {
        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            // Game folder structures are highly variable, so we accept all files
            return ImportSpecDecision.Accept();
        }
    }
}
