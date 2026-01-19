using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    public class UpgradeSpecification : IImportDecisionEngineSpecification
    {
        private readonly IConfigService _configService;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly Logger _logger;

        public UpgradeSpecification(IConfigService configService,
                                    ICustomFormatCalculationService formatService,
                                    Logger logger)
        {
            _configService = configService;
            _formatService = formatService;
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var downloadPropersAndRepacks = _configService.DownloadPropersAndRepacks;
            var qualityProfile = localGame.Game.QualityProfile;
            var qualityComparer = new QualityModelComparer(qualityProfile);

            if (localGame.Game.GameFileId > 0)
            {
                var gameFile = localGame.Game.GameFile;

                if (gameFile == null)
                {
                    _logger.Trace("Unable to get game file details from the DB. GameId: {0} GameFileId: {1}", localGame.Game.Id, localGame.Game.GameFileId);

                    return ImportSpecDecision.Accept();
                }

                var qualityCompare = qualityComparer.Compare(localGame.Quality.Quality, gameFile.Quality.Quality);

                if (qualityCompare < 0)
                {
                    _logger.Debug("This file isn't a quality upgrade for game. Existing quality: {0}. New Quality {1}. Skipping {2}", gameFile.Quality.Quality, localGame.Quality.Quality, localGame.Path);
                    return ImportSpecDecision.Reject(ImportRejectionReason.NotQualityUpgrade, "Not an upgrade for existing game file. Existing quality: {0}. New Quality {1}.", gameFile.Quality.Quality, localGame.Quality.Quality);
                }

                // Same quality, propers/repacks are preferred and it is not a revision update. Reject revision downgrade.

                if (qualityCompare == 0 &&
                    downloadPropersAndRepacks != ProperDownloadTypes.DoNotPrefer &&
                    localGame.Quality.Revision.CompareTo(gameFile.Quality.Revision) < 0)
                {
                    _logger.Debug("This file isn't a quality revision upgrade for game. Skipping {0}", localGame.Path);
                    return ImportSpecDecision.Reject(ImportRejectionReason.NotRevisionUpgrade, "Not a quality revision upgrade for existing game file(s)");
                }

                gameFile.Game = localGame.Game;
                var currentCustomFormats = _formatService.ParseCustomFormat(gameFile);
                var currentFormatScore = qualityProfile.CalculateCustomFormatScore(currentCustomFormats);
                var newCustomFormats = localGame.CustomFormats;
                var newFormatScore = localGame.CustomFormatScore;

                if (qualityCompare == 0 && newFormatScore < currentFormatScore)
                {
                    _logger.Debug("New item's custom formats [{0}] ({1}) do not improve on [{2}] ({3}), skipping",
                        newCustomFormats != null ? newCustomFormats.ConcatToString() : "",
                        newFormatScore,
                        currentCustomFormats != null ? currentCustomFormats.ConcatToString() : "",
                        currentFormatScore);

                    return ImportSpecDecision.Reject(ImportRejectionReason.NotCustomFormatUpgrade,
                        "Not a Custom Format upgrade for existing game file(s). New: [{0}] ({1}) do not improve on Existing: [{2}] ({3})",
                        newCustomFormats != null ? newCustomFormats.ConcatToString() : "",
                        newFormatScore,
                        currentCustomFormats != null ? currentCustomFormats.ConcatToString() : "",
                        currentFormatScore);
                }

                _logger.Debug("New item's custom formats [{0}] ({1}) do improve on [{2}] ({3}), accepting",
                    newCustomFormats != null ? newCustomFormats.ConcatToString() : "",
                    newFormatScore,
                    currentCustomFormats != null ? currentCustomFormats.ConcatToString() : "",
                    currentFormatScore);
            }

            return ImportSpecDecision.Accept();
        }
    }
}
