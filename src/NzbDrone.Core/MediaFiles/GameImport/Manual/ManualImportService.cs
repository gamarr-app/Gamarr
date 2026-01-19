using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.GameImport.Manual
{
    public interface IManualImportService
    {
        List<ManualImportItem> GetMediaFiles(int gameId);
        List<ManualImportItem> GetMediaFiles(string path, string downloadId, int? gameId, bool filterExistingFiles);
        ManualImportItem ReprocessItem(string path, string downloadId, int gameId, string releaseGroup, QualityModel quality, List<Language> languages, int indexerFlags);
    }

    public class ManualImportService : IExecute<ManualImportCommand>, IManualImportService
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IParsingService _parsingService;
        private readonly IDiskScanService _diskScanService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IGameService _gameService;
        private readonly IImportApprovedGame _importApprovedGame;
        private readonly IAggregationService _aggregationService;
        private readonly ITrackedDownloadService _trackedDownloadService;
        private readonly IDownloadedGameImportService _downloadedGameImportService;
        private readonly IMediaFileService _mediaFileService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public ManualImportService(IDiskProvider diskProvider,
                                   IParsingService parsingService,
                                   IDiskScanService diskScanService,
                                   IMakeImportDecision importDecisionMaker,
                                   IGameService gameService,
                                   IAggregationService aggregationService,
                                   IImportApprovedGame importApprovedGame,
                                   ITrackedDownloadService trackedDownloadService,
                                   IDownloadedGameImportService downloadedGameImportService,
                                   IMediaFileService mediaFileService,
                                   ICustomFormatCalculationService formatCalculator,
                                   IEventAggregator eventAggregator,
                                   Logger logger)
        {
            _diskProvider = diskProvider;
            _parsingService = parsingService;
            _diskScanService = diskScanService;
            _importDecisionMaker = importDecisionMaker;
            _gameService = gameService;
            _aggregationService = aggregationService;
            _importApprovedGame = importApprovedGame;
            _trackedDownloadService = trackedDownloadService;
            _downloadedGameImportService = downloadedGameImportService;
            _mediaFileService = mediaFileService;
            _formatCalculator = formatCalculator;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public List<ManualImportItem> GetMediaFiles(int gameId)
        {
            var game = _gameService.GetGame(gameId);
            var directoryInfo = new DirectoryInfo(game.Path);
            var gameFiles = _mediaFileService.GetFilesByGame(gameId);

            var items = gameFiles.Select(gameFile => MapItem(gameFile, game, directoryInfo.Name)).ToList();

            var mediaFiles = _diskScanService.FilterPaths(game.Path, _diskScanService.GetVideoFiles(game.Path)).ToList();
            var unmappedFiles = MediaFileService.FilterExistingFiles(mediaFiles, gameFiles, game);

            items.AddRange(unmappedFiles.Select(file =>
                new ManualImportItem
                {
                    Path = Path.Combine(game.Path, file),
                    FolderName = directoryInfo.Name,
                    RelativePath = game.Path.GetRelativePath(file),
                    Name = Path.GetFileNameWithoutExtension(file),
                    Game = game,
                    ReleaseGroup = string.Empty,
                    Quality = new QualityModel(Quality.Unknown),
                    Languages = new List<Language> { Language.Unknown },
                    Size = _diskProvider.GetFileSize(file),
                    Rejections = Enumerable.Empty<ImportRejection>()
                }));

            return items;
        }

        public List<ManualImportItem> GetMediaFiles(string path, string downloadId, int? gameId, bool filterExistingFiles)
        {
            if (downloadId.IsNotNullOrWhiteSpace())
            {
                var trackedDownload = _trackedDownloadService.Find(downloadId);

                if (trackedDownload == null)
                {
                    return new List<ManualImportItem>();
                }

                path = trackedDownload.ImportItem.OutputPath.FullPath;
            }

            if (!_diskProvider.FolderExists(path))
            {
                if (!_diskProvider.FileExists(path))
                {
                    return new List<ManualImportItem>();
                }

                var rootFolder = Path.GetDirectoryName(path);
                return new List<ManualImportItem> { ProcessFile(rootFolder, rootFolder, path, downloadId) };
            }

            return ProcessFolder(path, path, downloadId, gameId, filterExistingFiles);
        }

        public ManualImportItem ReprocessItem(string path, string downloadId, int gameId, string releaseGroup, QualityModel quality, List<Language> languages, int indexerFlags)
        {
            var rootFolder = Path.GetDirectoryName(path);
            var game = _gameService.GetGame(gameId);

            var languageParse = LanguageParser.ParseLanguages(path);

            if (languageParse.Count <= 1 && languageParse.First() == Language.Unknown && game != null)
            {
                languageParse = new List<Language> { game.GameMetadata.Value.OriginalLanguage };
                _logger.Debug("Language couldn't be parsed from release, fallback to game original language: {0}", game.GameMetadata.Value.OriginalLanguage.Name);
            }

            var downloadClientItem = GetTrackedDownload(downloadId)?.DownloadItem;
            var finalReleaseGroup = releaseGroup.IsNullOrWhiteSpace()
                ? ReleaseGroupParser.ParseReleaseGroup(path)
                : releaseGroup;
            var finalQuality = (quality?.Quality ?? Quality.Unknown) == Quality.Unknown ? QualityParser.ParseQuality(path) : quality;
            var finalLanguages =
                languages?.Count <= 1 && (languages?.SingleOrDefault() ?? Language.Unknown) == Language.Unknown
                    ? languageParse
                    : languages;

            var localGame = new LocalGame();
            localGame.Game = game;
            localGame.FileGameInfo = Parser.Parser.ParseGamePath(path);
            localGame.DownloadClientGameInfo = downloadClientItem == null ? null : Parser.Parser.ParseGameTitle(downloadClientItem.Title);
            localGame.DownloadItem = downloadClientItem;
            localGame.Path = path;
            localGame.SceneSource = SceneSource(game, rootFolder);
            localGame.ExistingFile = game.Path.IsParentPath(path);
            localGame.Size = _diskProvider.GetFileSize(path);
            localGame.ReleaseGroup = finalReleaseGroup;
            localGame.Languages = finalLanguages;
            localGame.Quality = finalQuality;
            localGame.IndexerFlags = (IndexerFlags)indexerFlags;

            localGame.CustomFormats = _formatCalculator.ParseCustomFormat(localGame);
            localGame.CustomFormatScore = localGame.Game?.QualityProfile?.CalculateCustomFormatScore(localGame.CustomFormats) ?? 0;

            // Augment game file so imported files have all additional information an automatic import would
            localGame = _aggregationService.Augment(localGame, downloadClientItem);

            // Reapply the user-chosen values.
            localGame.Game = game;
            localGame.ReleaseGroup = finalReleaseGroup;
            localGame.Quality = finalQuality;
            localGame.Languages = finalLanguages;
            localGame.IndexerFlags = (IndexerFlags)indexerFlags;

            return MapItem(_importDecisionMaker.GetDecision(localGame, downloadClientItem), rootFolder, downloadId, null);
        }

        private List<ManualImportItem> ProcessFolder(string rootFolder, string baseFolder, string downloadId, int? gameId, bool filterExistingFiles)
        {
            DownloadClientItem downloadClientItem = null;
            Game game = null;

            var directoryInfo = new DirectoryInfo(baseFolder);

            if (gameId.HasValue)
            {
                game = _gameService.GetGame(gameId.Value);
            }
            else
            {
                try
                {
                    game = _parsingService.GetGame(directoryInfo.Name);
                }
                catch (MultipleGamesFoundException e)
                {
                    _logger.Warn(e, "Unable to match game by title");
                }
            }

            if (downloadId.IsNotNullOrWhiteSpace())
            {
                var trackedDownload = _trackedDownloadService.Find(downloadId);
                downloadClientItem = trackedDownload.DownloadItem;

                if (game == null)
                {
                    game = trackedDownload.RemoteGame?.Game;
                }
            }

            if (game == null)
            {
                // Filter paths based on the rootFolder, so files in subfolders that should be ignored are ignored.
                // It will lead to some extra directories being checked for files, but it saves the processing of them and is cleaner than
                // teaching FilterPaths to know whether it's processing a file or a folder and changing it's filtering based on that.
                // If the game is unknown for the directory and there are more than 100 files in the folder don't process the items before returning.
                var files = _diskScanService.FilterPaths(rootFolder, _diskScanService.GetVideoFiles(baseFolder, false));

                if (files.Count > 100)
                {
                    _logger.Warn("Unable to determine game from folder name and found more than 100 files. Skipping parsing");

                    return ProcessDownloadDirectory(rootFolder, files);
                }

                var subfolders = _diskScanService.FilterPaths(rootFolder, _diskProvider.GetDirectories(baseFolder));

                var processedFiles = files.Select(file => ProcessFile(rootFolder, baseFolder, file, downloadId));
                var processedFolders = subfolders.SelectMany(subfolder => ProcessFolder(rootFolder, subfolder, downloadId, null, filterExistingFiles));

                return processedFiles.Concat(processedFolders).Where(i => i != null).ToList();
            }

            var folderInfo = Parser.Parser.ParseGameTitle(directoryInfo.Name);
            var gameFiles = _diskScanService.FilterPaths(rootFolder, _diskScanService.GetVideoFiles(baseFolder).ToList());
            var decisions = _importDecisionMaker.GetImportDecisions(gameFiles, game, downloadClientItem, folderInfo, SceneSource(game, baseFolder), filterExistingFiles);

            return decisions.Select(decision => MapItem(decision, rootFolder, downloadId, directoryInfo.Name)).ToList();
        }

        private ManualImportItem ProcessFile(string rootFolder, string baseFolder, string file, string downloadId, Game game = null)
        {
            try
            {
                var trackedDownload = GetTrackedDownload(downloadId);
                var relativeFile = baseFolder.GetRelativePath(file);

                if (game == null)
                {
                    game = _parsingService.GetGame(relativeFile.Split('\\', '/')[0]);
                }

                if (game == null)
                {
                    game = _parsingService.GetGame(relativeFile);
                }

                if (trackedDownload != null && game == null)
                {
                    game = trackedDownload?.RemoteGame?.Game;
                }

                if (game == null)
                {
                    var relativeParseInfo = Parser.Parser.ParseGamePath(relativeFile);

                    if (relativeParseInfo != null)
                    {
                        game = _gameService.FindByTitle(relativeParseInfo.PrimaryGameTitle, relativeParseInfo.Year);
                    }
                }

                if (game == null)
                {
                    var localGame = new LocalGame();
                    localGame.Path = file;
                    localGame.ReleaseGroup = ReleaseGroupParser.ParseReleaseGroup(file);
                    localGame.Quality = QualityParser.ParseQuality(file);
                    localGame.Languages = LanguageParser.ParseLanguages(file);
                    localGame.Size = _diskProvider.GetFileSize(file);

                    return MapItem(new ImportDecision(localGame,
                        new ImportRejection(ImportRejectionReason.UnknownGame, "Unknown Game")),
                        rootFolder,
                        downloadId,
                        null);
                }

                var importDecisions = _importDecisionMaker.GetImportDecisions(new List<string> { file }, game, trackedDownload?.DownloadItem, null, SceneSource(game, baseFolder));

                if (importDecisions.Any())
                {
                    return MapItem(importDecisions.First(), rootFolder, downloadId, null);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to process file: {0}", file);
            }

            return new ManualImportItem
            {
                DownloadId = downloadId,
                Path = file,
                RelativePath = rootFolder.GetRelativePath(file),
                Name = Path.GetFileNameWithoutExtension(file),
                Size = _diskProvider.GetFileSize(file),
                Rejections = new List<ImportRejection>()
            };
        }

        private List<ManualImportItem> ProcessDownloadDirectory(string rootFolder, List<string> videoFiles)
        {
            var items = new List<ManualImportItem>();

            foreach (var file in videoFiles)
            {
                var localGame = new LocalGame();
                localGame.Path = file;
                localGame.Quality = new QualityModel(Quality.Unknown);
                localGame.Languages = new List<Language> { Language.Unknown };
                localGame.ReleaseGroup = ReleaseGroupParser.ParseReleaseGroup(file);
                localGame.Size = _diskProvider.GetFileSize(file);

                items.Add(MapItem(new ImportDecision(localGame), rootFolder, null, null));
            }

            return items;
        }

        private bool SceneSource(Game game, string folder)
        {
            return !(game.Path.PathEquals(folder) || game.Path.IsParentPath(folder));
        }

        private TrackedDownload GetTrackedDownload(string downloadId)
        {
            if (downloadId.IsNotNullOrWhiteSpace())
            {
                var trackedDownload = _trackedDownloadService.Find(downloadId);

                return trackedDownload;
            }

            return null;
        }

        private ManualImportItem MapItem(ImportDecision decision, string rootFolder, string downloadId, string folderName)
        {
            var item = new ManualImportItem();

            item.Path = decision.LocalGame.Path;
            item.FolderName = folderName;
            item.RelativePath = rootFolder.GetRelativePath(decision.LocalGame.Path);
            item.Name = Path.GetFileNameWithoutExtension(decision.LocalGame.Path);
            item.DownloadId = downloadId;

            item.Quality = decision.LocalGame.Quality;
            item.Size = _diskProvider.GetFileSize(decision.LocalGame.Path);
            item.Languages = decision.LocalGame.Languages;
            item.ReleaseGroup = decision.LocalGame.ReleaseGroup;
            item.Rejections = decision.Rejections;
            item.IndexerFlags = (int)decision.LocalGame.IndexerFlags;

            if (decision.LocalGame.Game != null)
            {
                item.Game = decision.LocalGame.Game;

                item.CustomFormats = _formatCalculator.ParseCustomFormat(decision.LocalGame);
                item.CustomFormatScore = item.Game.QualityProfile?.CalculateCustomFormatScore(item.CustomFormats) ?? 0;
            }

            return item;
        }

        private ManualImportItem MapItem(GameFile gameFile, Game game, string folderName)
        {
            var item = new ManualImportItem();

            item.Path = Path.Combine(game.Path, gameFile.RelativePath);
            item.FolderName = folderName;
            item.RelativePath = gameFile.RelativePath;
            item.Name = Path.GetFileNameWithoutExtension(gameFile.Path);
            item.Game = game;
            item.ReleaseGroup = gameFile.ReleaseGroup;
            item.Quality = gameFile.Quality;
            item.Languages = gameFile.Languages;
            item.IndexerFlags = (int)gameFile.IndexerFlags;
            item.Size = _diskProvider.GetFileSize(item.Path);
            item.Rejections = Enumerable.Empty<ImportRejection>();
            item.GameFileId = gameFile.Id;
            item.CustomFormats = _formatCalculator.ParseCustomFormat(gameFile, game);

            return item;
        }

        public void Execute(ManualImportCommand message)
        {
            _logger.ProgressTrace("Manually importing {0} files using mode {1}", message.Files.Count, message.ImportMode);

            var imported = new List<ImportResult>();
            var importedTrackedDownload = new List<ManuallyImportedFile>();

            for (var i = 0; i < message.Files.Count; i++)
            {
                _logger.ProgressTrace("Processing file {0} of {1}", i + 1, message.Files.Count);

                var file = message.Files[i];
                var game = _gameService.GetGame(file.GameId);
                var fileGameInfo = Parser.Parser.ParseGamePath(file.Path) ?? new ParsedGameInfo();
                var existingFile = game.Path.IsParentPath(file.Path);
                TrackedDownload trackedDownload = null;

                var localGame = new LocalGame
                {
                    ExistingFile = existingFile,
                    FileGameInfo = fileGameInfo,
                    Path = file.Path,
                    ReleaseGroup = file.ReleaseGroup,
                    Quality = file.Quality,
                    Languages = file.Languages,
                    IndexerFlags = (IndexerFlags)file.IndexerFlags,
                    Game = game,
                    Size = 0
                };

                if (file.DownloadId.IsNotNullOrWhiteSpace())
                {
                    trackedDownload = _trackedDownloadService.Find(file.DownloadId);
                    localGame.DownloadClientGameInfo = trackedDownload?.RemoteGame?.ParsedGameInfo;
                    localGame.DownloadItem = trackedDownload?.DownloadItem;
                }

                if (file.FolderName.IsNotNullOrWhiteSpace())
                {
                    localGame.FolderGameInfo = Parser.Parser.ParseGameTitle(file.FolderName);
                    localGame.SceneSource = !existingFile;
                }

                // Augment game file so imported files have all additional information an automatic import would
                localGame = _aggregationService.Augment(localGame, trackedDownload?.DownloadItem);

                // Apply the user-chosen values.
                localGame.Game = game;
                localGame.ReleaseGroup = file.ReleaseGroup;
                localGame.Quality = file.Quality;
                localGame.Languages = file.Languages;
                localGame.IndexerFlags = (IndexerFlags)file.IndexerFlags;

                localGame.CustomFormats = _formatCalculator.ParseCustomFormat(localGame);
                localGame.CustomFormatScore = localGame.Game.QualityProfile?.CalculateCustomFormatScore(localGame.CustomFormats) ?? 0;

                // TODO: Cleanup non-tracked downloads
                var importDecision = new ImportDecision(localGame);

                if (trackedDownload == null)
                {
                    imported.AddRange(_importApprovedGame.Import(new List<ImportDecision> { importDecision }, !existingFile, null, message.ImportMode));
                }
                else
                {
                    var importResult = _importApprovedGame.Import(new List<ImportDecision> { importDecision }, true, trackedDownload.DownloadItem, message.ImportMode).First();

                    imported.Add(importResult);

                    importedTrackedDownload.Add(new ManuallyImportedFile
                    {
                        TrackedDownload = trackedDownload,
                        ImportResult = importResult
                    });
                }
            }

            _logger.ProgressTrace("Manually imported {0} files", imported.Count);

            foreach (var groupedTrackedDownload in importedTrackedDownload.GroupBy(i => i.TrackedDownload.DownloadItem.DownloadId).ToList())
            {
                var trackedDownload = groupedTrackedDownload.First().TrackedDownload;

                var importGame = groupedTrackedDownload.First().ImportResult.ImportDecision.LocalGame.Game;
                var outputPath = trackedDownload.ImportItem.OutputPath.FullPath;

                if (_diskProvider.FolderExists(outputPath))
                {
                    if (_downloadedGameImportService.ShouldDeleteFolder(
                            new DirectoryInfo(outputPath),
                            importGame) && trackedDownload.DownloadItem.CanMoveFiles)
                    {
                        _diskProvider.DeleteFolder(outputPath, true);
                    }
                }

                if (groupedTrackedDownload.Select(c => c.ImportResult).Any(c => c.Result == ImportResultType.Imported))
                {
                    trackedDownload.State = TrackedDownloadState.Imported;
                    _eventAggregator.PublishEvent(new DownloadCompletedEvent(trackedDownload, importGame.Id));
                }
            }
        }
    }
}
