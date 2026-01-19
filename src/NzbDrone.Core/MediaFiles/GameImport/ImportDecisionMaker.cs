using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
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
    }

    public class ImportDecisionMaker : IMakeImportDecision
    {
        private readonly IEnumerable<IImportDecisionEngineSpecification> _specifications;
        private readonly IMediaFileService _mediaFileService;
        private readonly IAggregationService _aggregationService;
        private readonly IDiskProvider _diskProvider;
        private readonly IDetectSample _detectSample;
        private readonly ITrackedDownloadService _trackedDownloadService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly Logger _logger;

        public ImportDecisionMaker(IEnumerable<IImportDecisionEngineSpecification> specifications,
                                   IMediaFileService mediaFileService,
                                   IAggregationService aggregationService,
                                   IDiskProvider diskProvider,
                                   IDetectSample detectSample,
                                   ITrackedDownloadService trackedDownloadService,
                                   ICustomFormatCalculationService formatCalculator,
                                   Logger logger)
        {
            _specifications = specifications;
            _mediaFileService = mediaFileService;
            _aggregationService = aggregationService;
            _diskProvider = diskProvider;
            _detectSample = detectSample;
            _trackedDownloadService = trackedDownloadService;
            _formatCalculator = formatCalculator;
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
            return videoFiles.Count(file =>
            {
                var sample = _detectSample.IsSample(game, file);

                if (sample == DetectSampleResult.Sample)
                {
                    return false;
                }

                return true;
            });
        }
    }
}
