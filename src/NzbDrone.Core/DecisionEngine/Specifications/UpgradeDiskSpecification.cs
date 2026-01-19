using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class UpgradeDiskSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly Logger _logger;

        public UpgradeDiskSpecification(UpgradableSpecification upgradableSpecification,
                                        ICustomFormatCalculationService formatService,
                                        Logger logger)
        {
            _upgradableSpecification = upgradableSpecification;
            _formatService = formatService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            var qualityProfile = subject.Game.QualityProfile;

            var file = subject.Game.GameFile;

            if (file == null)
            {
                _logger.Debug("File is no longer available, skipping this file.");
                return DownloadSpecDecision.Accept();
            }

            file.Game = subject.Game;

            var customFormats = _formatService.ParseCustomFormat(file);

            _logger.Debug("Comparing file quality with report. Existing file is {0} [{1}].", file.Quality, customFormats.ConcatToString());

            if (!_upgradableSpecification.CutoffNotMet(qualityProfile,
                    file.Quality,
                    _formatService.ParseCustomFormat(file),
                    subject.ParsedGameInfo.Quality))
            {
                // Cutoff is met, but still check if it's a version upgrade
                if (_upgradableSpecification.IsVersionUpgrade(file.GameVersion, subject.ParsedGameInfo.GameVersion))
                {
                    _logger.Debug(
                        "Cutoff met, but new release has newer game version {0} vs {1}, accepting",
                        subject.ParsedGameInfo.GameVersion,
                        file.GameVersion);
                }
                else
                {
                    _logger.Debug("Cutoff already met, rejecting.");

                    var cutoff = qualityProfile.UpgradeAllowed ? qualityProfile.Cutoff : qualityProfile.FirststAllowedQuality().Id;
                    var qualityCutoff = qualityProfile.Items[qualityProfile.GetIndex(cutoff).Index];

                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskCutoffMet, "Existing file meets cutoff: {0} [{1}]", qualityCutoff, customFormats.ConcatToString());
                }
            }

            var upgradeableRejectReason = _upgradableSpecification.IsUpgradable(qualityProfile,
                file.Quality,
                customFormats,
                subject.ParsedGameInfo.Quality,
                subject.CustomFormats);

            switch (upgradeableRejectReason)
            {
                case UpgradeableRejectReason.None:
                    return DownloadSpecDecision.Accept();

                case UpgradeableRejectReason.BetterQuality:
                    // Quality is lower, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(file.GameVersion, subject.ParsedGameInfo.GameVersion))
                    {
                        _logger.Debug(
                            "New release has newer game version {0} vs {1}, accepting despite lower quality",
                            subject.ParsedGameInfo.GameVersion,
                            file.GameVersion);
                        return DownloadSpecDecision.Accept();
                    }

                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskHigherPreference, "Existing file on disk is of equal or higher preference: {0}", file.Quality);

                case UpgradeableRejectReason.BetterRevision:
                    // Revision is lower, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(file.GameVersion, subject.ParsedGameInfo.GameVersion))
                    {
                        _logger.Debug(
                            "New release has newer game version {0} vs {1}, accepting despite lower revision",
                            subject.ParsedGameInfo.GameVersion,
                            file.GameVersion);
                        return DownloadSpecDecision.Accept();
                    }

                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskHigherRevision, "Existing file on disk is of equal or higher revision: {0}", file.Quality.Revision);

                case UpgradeableRejectReason.QualityCutoff:
                    // Quality cutoff met, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(file.GameVersion, subject.ParsedGameInfo.GameVersion))
                    {
                        _logger.Debug(
                            "New release has newer game version {0} vs {1}, accepting despite quality cutoff met",
                            subject.ParsedGameInfo.GameVersion,
                            file.GameVersion);
                        return DownloadSpecDecision.Accept();
                    }

                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskCutoffMet, "Existing file on disk meets quality cutoff: {0}", qualityProfile.Items[qualityProfile.GetIndex(qualityProfile.Cutoff).Index]);

                case UpgradeableRejectReason.CustomFormatCutoff:
                    // Custom format cutoff met, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(file.GameVersion, subject.ParsedGameInfo.GameVersion))
                    {
                        _logger.Debug(
                            "New release has newer game version {0} vs {1}, accepting despite custom format cutoff met",
                            subject.ParsedGameInfo.GameVersion,
                            file.GameVersion);
                        return DownloadSpecDecision.Accept();
                    }

                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskCustomFormatCutoffMet, "Existing file on disk meets Custom Format cutoff: {0}", qualityProfile.CutoffFormatScore);

                case UpgradeableRejectReason.CustomFormatScore:
                    // Custom format score is not better, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(file.GameVersion, subject.ParsedGameInfo.GameVersion))
                    {
                        _logger.Debug(
                            "New release has newer game version {0} vs {1}, accepting despite same custom format score",
                            subject.ParsedGameInfo.GameVersion,
                            file.GameVersion);
                        return DownloadSpecDecision.Accept();
                    }

                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskCustomFormatScore, "Existing file on disk has a equal or higher Custom Format score: {0}", qualityProfile.CalculateCustomFormatScore(customFormats));

                case UpgradeableRejectReason.MinCustomFormatScore:
                    // Min custom format score not met, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(file.GameVersion, subject.ParsedGameInfo.GameVersion))
                    {
                        _logger.Debug(
                            "New release has newer game version {0} vs {1}, accepting despite custom format score increment",
                            subject.ParsedGameInfo.GameVersion,
                            file.GameVersion);
                        return DownloadSpecDecision.Accept();
                    }

                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskCustomFormatScoreIncrement, "Existing file on disk has Custom Format score within Custom Format score increment: {0}", qualityProfile.MinUpgradeFormatScore);

                case UpgradeableRejectReason.UpgradesNotAllowed:
                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskUpgradesNotAllowed, "Existing file on disk and Quality Profile '{0}' does not allow upgrades", qualityProfile.Name);
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
