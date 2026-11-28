using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Extras.Others
{
    public class ExistingOtherExtraImporter : ImportExistingExtraFilesBase<OtherExtraFile>
    {
        private readonly IExtraFileService<OtherExtraFile> _otherExtraFileService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;

        public ExistingOtherExtraImporter(IExtraFileService<OtherExtraFile> otherExtraFileService,
                                          IMediaFileService mediaFileService,
                                          IParsingService parsingService,
                                          Logger logger)
            : base(otherExtraFileService)
        {
            _otherExtraFileService = otherExtraFileService;
            _mediaFileService = mediaFileService;
            _parsingService = parsingService;
            _logger = logger;
        }

        public override int Order => 2;

        public override IEnumerable<ExtraFile> ProcessFiles(Game game, List<string> filesOnDisk, List<string> importedFiles, string fileNameBeforeRename)
        {
            _logger.Debug("Looking for existing extra files in {0}", game.Path);

            var extraFiles = new List<OtherExtraFile>();
            var gameFiles = _mediaFileService.GetFilesByGame(game.Id);
            var filterResult = FilterAndClean(game, filesOnDisk, importedFiles, fileNameBeforeRename is not null);

            foreach (var possibleExtraFile in filterResult.FilesOnDisk)
            {
                var extension = Path.GetExtension(possibleExtraFile);

                if (extension.IsNullOrWhiteSpace())
                {
                    _logger.Debug("No extension for file: {0}", possibleExtraFile);
                    continue;
                }

                var minimalInfo = _parsingService.ParseMinimalPathGameInfo(possibleExtraFile);

                if (minimalInfo == null)
                {
                    _logger.Debug("Unable to parse extra file: {0}", possibleExtraFile);
                    continue;
                }

                var relativePath = game.Path.GetRelativePath(possibleExtraFile);

                var extraFile = new OtherExtraFile
                {
                    GameId = game.Id,
                    GameFileId = ExtraFileOwner.FindOwnerId(game, gameFiles, relativePath),
                    RelativePath = relativePath,
                    Extension = extension
                };

                extraFiles.Add(extraFile);
            }

            _logger.Info("Found {0} existing other extra files", extraFiles.Count);
            _otherExtraFileService.Upsert(extraFiles);

            // Return files that were just imported along with files that were
            // previously imported so previously imported files aren't imported twice
            return extraFiles.Concat(filterResult.PreviouslyImported);
        }
    }
}
