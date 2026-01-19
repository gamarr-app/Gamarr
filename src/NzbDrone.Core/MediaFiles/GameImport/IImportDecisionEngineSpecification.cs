using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport
{
    public interface IImportDecisionEngineSpecification
    {
        ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem);
    }
}
