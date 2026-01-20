using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Extras.Metadata
{
    public class ExistingMetadataImporter : ImportExistingExtraFilesBase<MetadataFile>
    {
        private readonly IExtraFileService<MetadataFile> _metadataFileService;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;
        private readonly List<IMetadata> _consumers;

        public ExistingMetadataImporter(IExtraFileService<MetadataFile> metadataFileService,
                                        IEnumerable<IMetadata> consumers,
                                        IParsingService parsingService,
                                        Logger logger)
        : base(metadataFileService)
        {
            _metadataFileService = metadataFileService;
            _parsingService = parsingService;
            _logger = logger;
            _consumers = consumers.ToList();
        }

        public override int Order => 0;

        public override IEnumerable<ExtraFile> ProcessFiles(Game game, List<string> filesOnDisk, List<string> importedFiles, string fileNameBeforeRename)
        {
            _logger.Debug("Looking for existing metadata in {0}", game.Path);

            var metadataFiles = new List<MetadataFile>();
            var filterResult = FilterAndClean(game, filesOnDisk, importedFiles, fileNameBeforeRename is not null);

            foreach (var possibleMetadataFile in filterResult.FilesOnDisk)
            {
                foreach (var consumer in _consumers)
                {
                    var metadata = consumer.FindMetadataFile(game, possibleMetadataFile);

                    if (metadata == null)
                    {
                        continue;
                    }

                    if (metadata.Type == MetadataType.GameImage ||
                        metadata.Type == MetadataType.GameMetadata)
                    {
                        var minimalInfo = _parsingService.ParseMinimalPathGameInfo(possibleMetadataFile);

                        if (minimalInfo == null)
                        {
                            _logger.Debug("Unable to parse extra file: {0}", possibleMetadataFile);
                            continue;
                        }

                        metadata.GameFileId = game.GameFileId;
                    }

                    metadata.Extension = Path.GetExtension(possibleMetadataFile);

                    metadataFiles.Add(metadata);
                }
            }

            _logger.Info("Found {0} existing metadata files", metadataFiles.Count);
            _metadataFileService.Upsert(metadataFiles);

            // Return files that were just imported along with files that were
            // previously imported so previously imported files aren't imported twice
            return metadataFiles.Concat(filterResult.PreviouslyImported);
        }
    }
}
