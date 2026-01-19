using System.IO;
using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    public class MatchesFolderSpecification : IImportDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MatchesFolderSpecification(Logger logger)
        {
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            if (localGame.ExistingFile)
            {
                return ImportSpecDecision.Accept();
            }

            var dirInfo = new FileInfo(localGame.Path).Directory;

            if (dirInfo == null)
            {
                return ImportSpecDecision.Accept();
            }

            // TODO: Actually implement this!!!!
            /*var folderInfo = Parser.Parser.ParseGameTitle(dirInfo.Name, false);

            if (folderInfo == null)
            {
                return Decision.Accept();
            }*/

            return ImportSpecDecision.Accept();
        }
    }
}
