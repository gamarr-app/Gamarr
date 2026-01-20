using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Extras.Metadata
{
    public class MetadataService : ExtraFileManager<MetadataFile>
    {
        private readonly IMetadataFactory _metadataFactory;
        private readonly ICleanMetadataService _cleanMetadataService;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IOtherExtraFileRenamer _otherExtraFileRenamer;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IHttpClient _httpClient;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IMetadataFileService _metadataFileService;
        private readonly Logger _logger;

        public MetadataService(IConfigService configService,
                               IDiskProvider diskProvider,
                               IDiskTransferService diskTransferService,
                               IRecycleBinProvider recycleBinProvider,
                               IOtherExtraFileRenamer otherExtraFileRenamer,
                               IMetadataFactory metadataFactory,
                               ICleanMetadataService cleanMetadataService,
                               IHttpClient httpClient,
                               IMediaFileAttributeService mediaFileAttributeService,
                               IMetadataFileService metadataFileService,
                               Logger logger)
            : base(configService, diskProvider, diskTransferService, logger)
        {
            _metadataFactory = metadataFactory;
            _cleanMetadataService = cleanMetadataService;
            _otherExtraFileRenamer = otherExtraFileRenamer;
            _recycleBinProvider = recycleBinProvider;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _httpClient = httpClient;
            _mediaFileAttributeService = mediaFileAttributeService;
            _metadataFileService = metadataFileService;
            _logger = logger;
        }

        public override int Order => 0;

        public override IEnumerable<ExtraFile> CreateAfterMediaCoverUpdate(Game game)
        {
            var metadataFiles = _metadataFileService.GetFilesByGame(game.Id);
            _cleanMetadataService.Clean(game);

            if (!_diskProvider.FolderExists(game.Path))
            {
                _logger.Info("Game folder does not exist, skipping metadata image creation");
                return Enumerable.Empty<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                files.AddRange(ProcessGameImages(consumer, game, consumerFiles));
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterGameScan(Game game, List<GameFile> gameFiles)
        {
            var metadataFiles = _metadataFileService.GetFilesByGame(game.Id);
            _cleanMetadataService.Clean(game);

            if (!_diskProvider.FolderExists(game.Path))
            {
                _logger.Info("Game folder does not exist, skipping metadata creation");
                return Enumerable.Empty<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                files.AddRange(ProcessGameImages(consumer, game, consumerFiles));

                foreach (var gameFile in gameFiles)
                {
                    files.AddIfNotNull(ProcessGameMetadata(consumer, game, gameFile, consumerFiles));
                }
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterGameImport(Game game, GameFile gameFile)
        {
            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                files.AddIfNotNull(ProcessGameMetadata(consumer, game, gameFile, new List<MetadataFile>()));
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterGameFolder(Game game, string gameFolder)
        {
            var metadataFiles = _metadataFileService.GetFilesByGame(game.Id);

            if (gameFolder.IsNullOrWhiteSpace())
            {
                return Array.Empty<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                if (gameFolder.IsNotNullOrWhiteSpace())
                {
                    files.AddRange(ProcessGameImages(consumer, game, consumerFiles));
                }
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> MoveFilesAfterRename(Game game, List<GameFile> gameFiles)
        {
            var metadataFiles = _metadataFileService.GetFilesByGame(game.Id);
            var movedFiles = new List<MetadataFile>();

            // TODO: Move GameImage and GameMetadata metadata files, instead of relying on consumers to do it
            // (Xbmc's GameImage is more than just the extension)
            foreach (var consumer in _metadataFactory.GetAvailableProviders())
            {
                foreach (var gameFile in gameFiles)
                {
                    var metadataFilesForConsumer = GetMetadataFilesForConsumer(consumer, metadataFiles).Where(m => m.GameFileId == gameFile.Id).ToList();

                    foreach (var metadataFile in metadataFilesForConsumer)
                    {
                        var newFileName = consumer.GetFilenameAfterMove(game, gameFile, metadataFile);
                        var existingFileName = Path.Combine(game.Path, metadataFile.RelativePath);

                        if (newFileName.PathNotEquals(existingFileName))
                        {
                            try
                            {
                                _diskProvider.MoveFile(existingFileName, newFileName);
                                metadataFile.RelativePath = game.Path.GetRelativePath(newFileName);
                                movedFiles.Add(metadataFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Unable to move metadata file after rename: {0}", existingFileName);
                            }
                        }
                    }
                }
            }

            _metadataFileService.Upsert(movedFiles);

            return movedFiles;
        }

        public override bool CanImportFile(LocalGame localGame, GameFile gameFile, string path, string extension, bool readOnly)
        {
            return false;
        }

        public override IEnumerable<ExtraFile> ImportFiles(LocalGame localGame, GameFile gameFile, List<string> files, bool isReadOnly)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        private List<MetadataFile> GetMetadataFilesForConsumer(IMetadata consumer, List<MetadataFile> gameMetadata)
        {
            return gameMetadata.Where(c => c.Consumer == consumer.GetType().Name).ToList();
        }

        private MetadataFile ProcessGameMetadata(IMetadata consumer, Game game, GameFile gameFile, List<MetadataFile> existingMetadataFiles)
        {
            var gameFileMetadata = consumer.GameMetadata(game, gameFile);

            if (gameFileMetadata == null)
            {
                return null;
            }

            var fullPath = Path.Combine(game.Path, gameFileMetadata.RelativePath);

            _otherExtraFileRenamer.RenameOtherExtraFile(game, fullPath);

            var existingMetadata = GetMetadataFile(game, existingMetadataFiles, c => c.Type == MetadataType.GameMetadata &&
                                                                                  c.GameFileId == gameFile.Id);

            if (existingMetadata != null)
            {
                var existingFullPath = Path.Combine(game.Path, existingMetadata.RelativePath);
                if (fullPath.PathNotEquals(existingFullPath))
                {
                    _diskTransferService.TransferFile(existingFullPath, fullPath, TransferMode.Move);
                    existingMetadata.RelativePath = gameFileMetadata.RelativePath;
                }
            }

            var hash = gameFileMetadata.Contents.SHA256Hash();

            var metadata = existingMetadata ??
                           new MetadataFile
                           {
                               GameId = game.Id,
                               GameFileId = gameFile.Id,
                               Consumer = consumer.GetType().Name,
                               Type = MetadataType.GameMetadata,
                               RelativePath = gameFileMetadata.RelativePath,
                               Extension = Path.GetExtension(fullPath)
                           };

            if (hash == metadata.Hash)
            {
                return null;
            }

            _logger.Debug("Writing Game File Metadata to: {0}", fullPath);
            SaveMetadataFile(fullPath, gameFileMetadata.Contents);

            metadata.Hash = hash;

            return metadata;
        }

        private List<MetadataFile> ProcessGameImages(IMetadata consumer, Game game, List<MetadataFile> existingMetadataFiles)
        {
            var result = new List<MetadataFile>();

            foreach (var image in consumer.GameImages(game))
            {
                var fullPath = Path.Combine(game.Path, image.RelativePath);

                if (_diskProvider.FileExists(fullPath))
                {
                    _logger.Debug("Game image already exists: {0}", fullPath);
                    continue;
                }

                _otherExtraFileRenamer.RenameOtherExtraFile(game, fullPath);

                var metadata = GetMetadataFile(game, existingMetadataFiles, c => c.Type == MetadataType.GameImage &&
                                                                                   c.RelativePath == image.RelativePath) ??
                               new MetadataFile
                               {
                                   GameId = game.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.GameImage,
                                   RelativePath = image.RelativePath,
                                   Extension = Path.GetExtension(fullPath)
                               };

                DownloadImage(game, image);

                result.Add(metadata);
            }

            return result;
        }

        private void DownloadImage(Game game, ImageFileResult image)
        {
            var fullPath = Path.Combine(game.Path, image.RelativePath);
            var downloaded = true;

            try
            {
                if (image.Url.StartsWith("http"))
                {
                    _httpClient.DownloadFile(image.Url, fullPath);
                }
                else if (_diskProvider.FileExists(image.Url))
                {
                    _diskProvider.CopyFile(image.Url, fullPath);
                }
                else
                {
                    downloaded = false;
                }

                if (downloaded)
                {
                    _mediaFileAttributeService.SetFilePermissions(fullPath);
                }
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, "Couldn't download image {0} for {1}. {2}", image.Url, game, ex.Message);
            }
            catch (WebException ex)
            {
                _logger.Warn(ex, "Couldn't download image {0} for {1}. {2}", image.Url, game, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Couldn't download image {0} for {1}. {2}", image.Url, game, ex.Message);
            }
        }

        private void SaveMetadataFile(string path, string contents)
        {
            _diskProvider.WriteAllText(path, contents);
            _mediaFileAttributeService.SetFilePermissions(path);
        }

        private MetadataFile GetMetadataFile(Game game, List<MetadataFile> existingMetadataFiles, Func<MetadataFile, bool> predicate)
        {
            var matchingMetadataFiles = existingMetadataFiles.Where(predicate).ToList();

            if (matchingMetadataFiles.Empty())
            {
                return null;
            }

            // Remove duplicate metadata files from DB and disk
            foreach (var file in matchingMetadataFiles.Skip(1))
            {
                var path = Path.Combine(game.Path, file.RelativePath);

                _logger.Debug("Removing duplicate Metadata file: {0}", path);

                _recycleBinProvider.DeleteFile(path);
                _metadataFileService.Delete(file.Id);
            }

            return matchingMetadataFiles.First();
        }
    }
}
