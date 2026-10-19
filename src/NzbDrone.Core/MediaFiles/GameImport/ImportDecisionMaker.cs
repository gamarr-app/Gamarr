using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.MediaFiles.Archives;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport
{
    public interface IMakeImportDecision
    {
        List<ImportDecision> GetImportDecisions(List<string> videoFiles, Game game);
        List<ImportDecision> GetImportDecisions(List<string> videoFiles, Game game, bool filterExistingFiles);
        List<ImportDecision> GetImportDecisions(List<string> videoFiles, Game game, DownloadClientItem downloadClientItem, ParsedGameInfo folderInfo, bool sceneSource);
        List<ImportDecision> GetImportDecisions(List<string> videoFiles, Game game, DownloadClientItem downloadClientItem, ParsedGameInfo folderInfo, bool sceneSource, bool filterExistingFiles);
        ImportDecision GetDecision(LocalGame localGame, DownloadClientItem downloadClientItem);
        void InspectArchive(LocalGame localGame);
    }

    public class ImportDecisionMaker : IMakeImportDecision
    {
        private readonly IEnumerable<IImportDecisionEngineSpecification> _specifications;
        private readonly IMediaFileService _mediaFileService;
        private readonly IAggregationService _aggregationService;
        private readonly IDiskProvider _diskProvider;
        private readonly ITrackedDownloadService _trackedDownloadService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IGameArchiveService _archiveService;
        private readonly Logger _logger;

        public ImportDecisionMaker(IEnumerable<IImportDecisionEngineSpecification> specifications,
                                   IMediaFileService mediaFileService,
                                   IAggregationService aggregationService,
                                   IDiskProvider diskProvider,
                                   ITrackedDownloadService trackedDownloadService,
                                   ICustomFormatCalculationService formatCalculator,
                                   IGameArchiveService archiveService,
                                   Logger logger)
        {
            _specifications = specifications;
            _mediaFileService = mediaFileService;
            _aggregationService = aggregationService;
            _diskProvider = diskProvider;
            _trackedDownloadService = trackedDownloadService;
            _formatCalculator = formatCalculator;
            _archiveService = archiveService;
            _logger = logger;
        }

        public List<ImportDecision> GetImportDecisions(List<string> videoFiles, Game game)
        {
            return GetImportDecisions(videoFiles, game, null, null, false);
        }

        public List<ImportDecision> GetImportDecisions(List<string> videoFiles, Game game, bool filterExistingFiles)
        {
            return GetImportDecisions(videoFiles, game, null, null, false, filterExistingFiles);
        }

        public List<ImportDecision> GetImportDecisions(List<string> videoFiles, Game game, DownloadClientItem downloadClientItem, ParsedGameInfo folderInfo, bool sceneSource)
        {
            return GetImportDecisions(videoFiles, game, downloadClientItem, folderInfo, sceneSource, true);
        }

        public List<ImportDecision> GetImportDecisions(List<string> videoFiles, Game game, DownloadClientItem downloadClientItem, ParsedGameInfo folderInfo, bool sceneSource, bool filterExistingFiles)
        {
            var newFiles = filterExistingFiles ? _mediaFileService.FilterExistingFiles(videoFiles.ToList(), game) : videoFiles.ToList();

            _logger.Debug("Analyzing {0}/{1} files.", newFiles.Count, videoFiles.Count);

            ParsedGameInfo downloadClientItemInfo = null;

            if (downloadClientItem != null)
            {
                downloadClientItemInfo = Parser.Parser.ParseGameTitle(downloadClientItem.Title);
            }

            var nonSampleVideoFileCount = GetNonSampleVideoFileCount(newFiles, game.GameMetadata);

            var decisions = new List<ImportDecision>();

            foreach (var file in newFiles)
            {
                var localGame = new LocalGame
                {
                    Game = game,
                    DownloadClientGameInfo = downloadClientItemInfo,
                    DownloadItem = downloadClientItem,
                    FolderGameInfo = folderInfo,
                    Path = file,
                    SceneSource = sceneSource,
                    ExistingFile = game.Path.IsParentPath(file),
                    OtherVideoFiles = nonSampleVideoFileCount > 1
                };

                decisions.AddIfNotNull(GetDecision(localGame, downloadClientItem, nonSampleVideoFileCount > 1));
            }

            return decisions;
        }

        /// <summary>
        /// Phase one of archive-wrapped import: a cheap header-only peek. When the
        /// release is nothing but an archive around a single game file, Path is
        /// pointed at the archive — which makes the import unit one file instead of
        /// an opaque folder, so release junk (filler .pad dirs, html, metadata) is
        /// left behind — and Size is replaced with the uncompressed entry size.
        ///
        /// It has to happen here rather than in a spec: specs can only accept or
        /// reject, and FreeSpaceSpecification, the custom format calculator and the
        /// file namer all need the corrected size and extension before they run.
        /// Nothing is decompressed; extraction happens in the mover, on approval.
        /// </summary>
        public void InspectArchive(LocalGame localGame)
        {
            var allowWholeArchive = AllowsWholeArchiveExtraction(localGame.Game);

            var inspection = _archiveService.InspectImportUnit(localGame.Path, allowWholeArchive);

            if (inspection == null)
            {
                return;
            }

            if (inspection.ExtractWholeArchive)
            {
                _logger.Debug("Import unit {0} is an archive with no single extractable game file; extracting all of it ({1} bytes) into the game folder",
                    localGame.Path,
                    inspection.EntrySize);
            }
            else
            {
                _logger.Debug("Import unit {0} is an archive-wrapped release; importing {1} ({2} bytes) instead",
                    localGame.Path,
                    inspection.EntryName,
                    inspection.EntrySize);
            }

            localGame.ArchiveInspection = inspection;
            localGame.Path = inspection.ArchivePath;
            localGame.Size = inspection.EntrySize;
        }

        /// <summary>
        /// Whether an archive holding no single extractable ROM entry may be
        /// extracted wholesale into the game folder.
        ///
        /// This gate lives here and not in <see cref="IGameArchiveService"/>
        /// because it depends on the game, which that service does not see. PC
        /// (and Linux/Mac) releases are an installer tree — exe, data files,
        /// subfolders — that no single-file extract can represent, and those
        /// games already import at folder granularity, so the whole tree is the
        /// right unit. <see cref="PlatformFamily.Unknown"/> is eligible too and
        /// that is load-bearing: a per-entry platform is optional and most PC
        /// entries in a real library never set one. A console platform keeps the
        /// original exactly-one-ROM-entry rule, where collapsing a release into
        /// one file is both possible and what the library scanner wants.
        /// </summary>
        private static bool AllowsWholeArchiveExtraction(Game game)
        {
            if (game == null)
            {
                return true;
            }

            return game.Platform is PlatformFamily.Unknown
                or PlatformFamily.PC
                or PlatformFamily.Linux
                or PlatformFamily.Mac;
        }

        public ImportDecision GetDecision(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var reasons = _specifications.Select(c => EvaluateSpec(c, localGame, downloadClientItem))
                                         .Where(c => c != null);

            return new ImportDecision(localGame, reasons.ToArray());
        }

        private ImportDecision GetDecision(LocalGame localGame, DownloadClientItem downloadClientItem, bool otherFiles)
        {
            ImportDecision decision = null;

            try
            {
                var fileGameInfo = Parser.Parser.ParseGamePath(localGame.Path);

                localGame.FileGameInfo = fileGameInfo;

                // Handle both files and folders (games are typically folders)
                if (_diskProvider.FolderExists(localGame.Path))
                {
                    localGame.Size = _diskProvider.GetFolderSize(localGame.Path);
                }
                else
                {
                    localGame.Size = _diskProvider.GetFileSize(localGame.Path);
                }

                // Must run after the path parse (the folder name carries the full
                // release title) and before Augment/specs, which need the real size.
                InspectArchive(localGame);

                _aggregationService.Augment(localGame, downloadClientItem);

                if (localGame.Game == null)
                {
                    decision = new ImportDecision(localGame, new ImportRejection(ImportRejectionReason.InvalidGame, "Invalid game"));
                }
                else
                {
                    if (downloadClientItem?.DownloadId.IsNotNullOrWhiteSpace() == true)
                    {
                        var trackedDownload = _trackedDownloadService.Find(downloadClientItem.DownloadId);

                        if (trackedDownload?.RemoteGame?.Release?.IndexerFlags != null)
                        {
                            localGame.IndexerFlags = trackedDownload.RemoteGame.Release.IndexerFlags;
                        }
                    }

                    localGame.CustomFormats = _formatCalculator.ParseCustomFormat(localGame);
                    localGame.CustomFormatScore = localGame.Game.QualityProfile?.CalculateCustomFormatScore(localGame.CustomFormats) ?? 0;

                    decision = GetDecision(localGame, downloadClientItem);
                }
            }
            catch (AugmentingFailedException)
            {
                decision = new ImportDecision(localGame, new ImportRejection(ImportRejectionReason.UnableToParse, "Unable to parse file"));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Couldn't import file. {0}", localGame.Path);

                decision = new ImportDecision(localGame, new ImportRejection(ImportRejectionReason.Error, "Unexpected error processing file"));
            }

            if (decision == null)
            {
                _logger.Error("Unable to make a decision on {0}", localGame.Path);
            }
            else if (decision.Rejections.Any())
            {
                _logger.Debug("File rejected for the following reasons: {0}", string.Join(", ", decision.Rejections));
            }
            else
            {
                _logger.Debug("File accepted");
            }

            return decision;
        }

        private ImportRejection EvaluateSpec(IImportDecisionEngineSpecification spec, LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            try
            {
                var result = spec.IsSatisfiedBy(localGame, downloadClientItem);

                if (!result.Accepted)
                {
                    return new ImportRejection(result.Reason, result.Message);
                }
            }
            catch (NotImplementedException e)
            {
                _logger.Warn(e, "Spec " + spec.ToString() + " currently does not implement evaluation for games.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Couldn't evaluate decision on {0}", localGame.Path);
                return new ImportRejection(ImportRejectionReason.DecisionError, $"{spec.GetType().Name}: {ex.Message}");
            }

            return null;
        }

        private int GetNonSampleVideoFileCount(List<string> videoFiles, GameMetadata game)
        {
            return videoFiles.Count;
        }
    }
}
