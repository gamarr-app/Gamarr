using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Extras.Subtitles
{
    public class ExistingSubtitleImporter : ImportExistingExtraFilesBase<SubtitleFile>
    {
        private readonly IExtraFileService<SubtitleFile> _subtitleFileService;
        private readonly IAggregationService _aggregationService;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;

        public ExistingSubtitleImporter(IExtraFileService<SubtitleFile> subtitleFileService,
                                        IAggregationService aggregationService,
                                        IParsingService parsingService,
                                        Logger logger)
            : base(subtitleFileService)
        {
            _subtitleFileService = subtitleFileService;
            _aggregationService = aggregationService;
            _parsingService = parsingService;
            _logger = logger;
        }

        public override int Order => 1;

        public override IEnumerable<ExtraFile> ProcessFiles(Game game, List<string> filesOnDisk, List<string> importedFiles, string fileNameBeforeRename)
        {
            _logger.Debug("Looking for existing subtitle files in {0}", game.Path);

            var subtitleFiles = new List<SubtitleFile>();
            var filterResult = FilterAndClean(game, filesOnDisk, importedFiles, fileNameBeforeRename is not null);

            foreach (var possibleSubtitleFile in filterResult.FilesOnDisk)
            {
                var extension = Path.GetExtension(possibleSubtitleFile);

                if (SubtitleFileExtensions.Extensions.Contains(extension))
                {
                    var minimalInfo = _parsingService.ParseMinimalPathGameInfo(possibleSubtitleFile);

                    if (minimalInfo == null)
                    {
                        _logger.Debug("Unable to parse subtitle file: {0}", possibleSubtitleFile);
                        continue;
                    }

                    var localGame = new LocalGame
                    {
                        FileGameInfo = minimalInfo,
                        Game = game,
                        Path = possibleSubtitleFile,
                        FileNameBeforeRename = fileNameBeforeRename
                    };

                    try
                    {
                        _aggregationService.Augment(localGame, null);
                    }
                    catch (AugmentingFailedException)
                    {
                        _logger.Debug("Unable to parse extra file: {0}", possibleSubtitleFile);
                        continue;
                    }

                    var subtitleFile = new SubtitleFile
                    {
                        GameId = game.Id,
                        GameFileId = game.GameFileId,
                        RelativePath = game.Path.GetRelativePath(possibleSubtitleFile),
                        Language = localGame.SubtitleInfo.Language,
                        LanguageTags = localGame.SubtitleInfo.LanguageTags,
                        Title = localGame.SubtitleInfo.Title,
                        Extension = extension,
                        Copy = localGame.SubtitleInfo.Copy
                    };

                    subtitleFiles.Add(subtitleFile);
                }
            }

            _logger.Info("Found {0} existing subtitle files", subtitleFiles.Count);
            _subtitleFileService.Upsert(subtitleFiles);

            // Return files that were just imported along with files that were
            // previously imported so previously imported files aren't imported twice
            return subtitleFiles.Concat(filterResult.PreviouslyImported);
        }
    }
}
