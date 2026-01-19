using System.Collections.Generic;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Extras
{
    public interface IImportExistingExtraFiles
    {
        int Order { get; }
        IEnumerable<ExtraFile> ProcessFiles(Game game, List<string> filesOnDisk, List<string> importedFiles, string fileNameBeforeRename);
    }
}
