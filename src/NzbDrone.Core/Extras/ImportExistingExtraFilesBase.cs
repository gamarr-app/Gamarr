using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Extras
{
    public abstract class ImportExistingExtraFilesBase<TExtraFile> : IImportExistingExtraFiles
        where TExtraFile : ExtraFile, new()
    {
        private readonly IExtraFileService<TExtraFile> _extraFileService;

        public ImportExistingExtraFilesBase(IExtraFileService<TExtraFile> extraFileService)
        {
            _extraFileService = extraFileService;
        }

        public abstract int Order { get; }
        public abstract IEnumerable<ExtraFile> ProcessFiles(Game game, List<string> filesOnDisk, List<string> importedFiles, string fileNameBeforeRename);

        public virtual ImportExistingExtraFileFilterResult<TExtraFile> FilterAndClean(Game game, List<string> filesOnDisk, List<string> importedFiles, bool keepExistingEntries)
        {
            var gameFiles = _extraFileService.GetFilesByGame(game.Id);

            if (keepExistingEntries)
            {
                var incompleteImports = gameFiles.IntersectBy(f => Path.Combine(game.Path, f.RelativePath), filesOnDisk, i => i, PathEqualityComparer.Instance).Select(f => f.Id);

                _extraFileService.DeleteMany(incompleteImports);

                return Filter(game, filesOnDisk, importedFiles, new List<TExtraFile>());
            }

            Clean(game, filesOnDisk, importedFiles, gameFiles);

            return Filter(game, filesOnDisk, importedFiles, gameFiles);
        }

        private ImportExistingExtraFileFilterResult<TExtraFile> Filter(Game game, List<string> filesOnDisk, List<string> importedFiles, List<TExtraFile> gameFiles)
        {
            var previouslyImported = gameFiles.IntersectBy(s => Path.Combine(game.Path, s.RelativePath), filesOnDisk, f => f, PathEqualityComparer.Instance).ToList();
            var filteredFiles = filesOnDisk.Except(previouslyImported.Select(f => Path.Combine(game.Path, f.RelativePath)).ToList(), PathEqualityComparer.Instance)
                                           .Except(importedFiles, PathEqualityComparer.Instance)
                                           .ToList();

            // Return files that are already imported so they aren't imported again by other importers.
            // Filter out files that were previously imported and as well as ones imported by other importers.
            return new ImportExistingExtraFileFilterResult<TExtraFile>(previouslyImported, filteredFiles);
        }

        private void Clean(Game game, List<string> filesOnDisk, List<string> importedFiles, List<TExtraFile> gameFiles)
        {
            var alreadyImportedFileIds = gameFiles.IntersectBy(f => Path.Combine(game.Path, f.RelativePath), importedFiles, i => i, PathEqualityComparer.Instance)
                .Select(f => f.Id);

            var deletedFiles = gameFiles.ExceptBy(f => Path.Combine(game.Path, f.RelativePath), filesOnDisk, i => i, PathEqualityComparer.Instance)
                .Select(f => f.Id);

            _extraFileService.DeleteMany(alreadyImportedFileIds);
            _extraFileService.DeleteMany(deletedFiles);
        }
    }
}
