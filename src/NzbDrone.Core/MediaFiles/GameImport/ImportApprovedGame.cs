using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Extras;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.GameImport
{
    public interface IImportApprovedGame
    {
        List<ImportResult> Import(List<ImportDecision> decisions, bool newDownload, DownloadClientItem downloadClientItem = null, ImportMode importMode = ImportMode.Auto);
    }

    public class ImportApprovedGame : IImportApprovedGame
    {
        private readonly IUpgradeMediaFiles _gameFileUpgrader;
        private readonly IMediaFileService _mediaFileService;
        private readonly IExtraService _extraService;
        private readonly IExistingExtraFiles _existingExtraFiles;
        private readonly IDiskProvider _diskProvider;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportApprovedGame(IUpgradeMediaFiles gameFileUpgrader,
                                   IMediaFileService mediaFileService,
                                   IExtraService extraService,
                                   IExistingExtraFiles existingExtraFiles,
                                   IDiskProvider diskProvider,
                                   IHistoryService historyService,
                                   IEventAggregator eventAggregator,
                                   IManageCommandQueue commandQueueManager,
                                   Logger logger)
        {
            _gameFileUpgrader = gameFileUpgrader;
            _mediaFileService = mediaFileService;
            _extraService = extraService;
            _existingExtraFiles = existingExtraFiles;
            _diskProvider = diskProvider;
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public List<ImportResult> Import(List<ImportDecision> decisions, bool newDownload, DownloadClientItem downloadClientItem = null, ImportMode importMode = ImportMode.Auto)
        {
            _logger.Debug("Decisions: {0}", decisions.Count);

            // I added a null op for the rare case that the quality is null. TODO: find out why that would even happen in the first place.
            var qualifiedImports = decisions
                .Where(decision => decision.Approved)
                .GroupBy(decision => decision.LocalGame.Game.Id)
                .SelectMany(group => group
                    .OrderByDescending(decision => decision.LocalGame.Quality ?? new QualityModel { Quality = Quality.Unknown }, new QualityModelComparer(group.First().LocalGame.Game.QualityProfile))
                    .ThenByDescending(decision => decision.LocalGame.Size))
                .ToList();

            var importResults = new List<ImportResult>();

            foreach (var importDecision in qualifiedImports.OrderByDescending(e => e.LocalGame.Size))
            {
                var localGame = importDecision.LocalGame;
                var oldFiles = new List<DeletedGameFile>();

                try
                {
                    // check if already imported
                    if (importResults.Select(r => r.ImportDecision.LocalGame.Game)
                                         .Select(m => m.Id).Contains(localGame.Game.Id))
                    {
                        importResults.Add(new ImportResult(importDecision, "Game has already been imported"));
                        continue;
                    }

                    var gameFile = new GameFile();
                    gameFile.DateAdded = DateTime.UtcNow;
                    gameFile.GameId = localGame.Game.Id;
                    gameFile.Path = localGame.Path.CleanFilePath();
                    gameFile.Size = _diskProvider.GetFileSize(localGame.Path);
                    gameFile.Quality = localGame.Quality;
                    gameFile.Languages = localGame.Languages;
                    gameFile.MediaInfo = localGame.MediaInfo;
                    gameFile.Game = localGame.Game;
                    gameFile.ReleaseGroup = localGame.ReleaseGroup;
                    gameFile.Edition = localGame.Edition;

                    if (downloadClientItem?.DownloadId.IsNotNullOrWhiteSpace() == true)
                    {
                        var grabHistory = _historyService.FindByDownloadId(downloadClientItem.DownloadId)
                            .OrderByDescending(h => h.Date)
                            .FirstOrDefault(h => h.EventType == GameHistoryEventType.Grabbed);

                        if (Enum.TryParse(grabHistory?.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                        {
                            gameFile.IndexerFlags = flags;
                        }
                    }
                    else
                    {
                        gameFile.IndexerFlags = localGame.IndexerFlags;
                    }

                    bool copyOnly;
                    switch (importMode)
                    {
                        default:
                        case ImportMode.Auto:
                            copyOnly = downloadClientItem is { CanMoveFiles: false };
                            break;
                        case ImportMode.Move:
                            copyOnly = false;
                            break;
                        case ImportMode.Copy:
                            copyOnly = true;
                            break;
                    }

                    if (newDownload)
                    {
                        gameFile.SceneName = localGame.SceneName;
                        gameFile.OriginalFilePath = GetOriginalFilePath(downloadClientItem, localGame);

                        oldFiles = _gameFileUpgrader.UpgradeGameFile(gameFile, localGame, copyOnly).OldFiles;
                    }
                    else
                    {
                        gameFile.RelativePath = localGame.Game.Path.GetRelativePath(gameFile.Path);

                        // Delete existing files from the DB mapped to this path
                        var previousFiles = _mediaFileService.GetFilesWithRelativePath(localGame.Game.Id, gameFile.RelativePath);

                        foreach (var previousFile in previousFiles)
                        {
                            _mediaFileService.Delete(previousFile, DeleteMediaFileReason.ManualOverride);
                        }
                    }

                    gameFile = _mediaFileService.Add(gameFile);
                    importResults.Add(new ImportResult(importDecision));

                    localGame.Game.GameFile = gameFile;

                    if (newDownload)
                    {
                        if (localGame.ScriptImported)
                        {
                            _existingExtraFiles.ImportExtraFiles(localGame.Game, localGame.PossibleExtraFiles, localGame.FileNameBeforeRename);

                            if (localGame.FileNameBeforeRename != gameFile.RelativePath)
                            {
                                _extraService.MoveFilesAfterRename(localGame.Game, gameFile);
                            }
                        }

                        if (!localGame.ScriptImported || localGame.ShouldImportExtras)
                        {
                            _extraService.ImportGame(localGame, gameFile, copyOnly);
                        }
                    }

                    _eventAggregator.PublishEvent(new GameFileImportedEvent(localGame, gameFile, oldFiles, newDownload, downloadClientItem));
                }
                catch (RootFolderNotFoundException e)
                {
                    _logger.Warn(e, "Couldn't import game " + localGame);
                    _eventAggregator.PublishEvent(new GameImportFailedEvent(e, localGame, newDownload, downloadClientItem));

                    importResults.Add(new ImportResult(importDecision, "Failed to import game, Root folder missing."));
                }
                catch (DestinationAlreadyExistsException e)
                {
                    _logger.Warn(e, "Couldn't import game " + localGame);
                    importResults.Add(new ImportResult(importDecision, "Failed to import game, Destination already exists."));

                    _commandQueueManager.Push(new RescanGameCommand(localGame.Game.Id));
                }
                catch (RecycleBinException e)
                {
                    _logger.Warn(e, "Couldn't import game " + localGame);
                    _eventAggregator.PublishEvent(new GameImportFailedEvent(e, localGame, newDownload, downloadClientItem));

                    importResults.Add(new ImportResult(importDecision, "Failed to import game, unable to move existing file to the Recycle Bin."));
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Couldn't import game " + localGame);
                    importResults.Add(new ImportResult(importDecision, "Failed to import game"));
                }
            }

            // Adding all the rejected decisions
            importResults.AddRange(decisions.Where(c => !c.Approved)
                                            .Select(d => new ImportResult(d, d.Rejections.Select(r => r.Message).ToArray())));

            return importResults;
        }

        private string GetOriginalFilePath(DownloadClientItem downloadClientItem, LocalGame localGame)
        {
            var path = localGame.Path;

            if (downloadClientItem != null && !downloadClientItem.OutputPath.IsEmpty)
            {
                var outputDirectory = downloadClientItem.OutputPath.Directory.ToString();

                if (outputDirectory.IsParentPath(path))
                {
                    return outputDirectory.GetRelativePath(path);
                }
            }

            var folderGameInfo = localGame.FolderGameInfo;

            if (folderGameInfo != null)
            {
                var folderPath = path.GetAncestorPath(folderGameInfo.OriginalTitle);

                if (folderPath != null)
                {
                    return folderPath.GetParentPath().GetRelativePath(path);
                }
            }

            var parentPath = path.GetParentPath();
            var grandparentPath = parentPath.GetParentPath();

            if (grandparentPath != null)
            {
                return grandparentPath.GetRelativePath(path);
            }

            return Path.GetFileName(path);
        }
    }
}
