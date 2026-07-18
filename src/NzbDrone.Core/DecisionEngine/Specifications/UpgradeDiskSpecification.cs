using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class UpgradeDiskSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public UpgradeDiskSpecification(UpgradableSpecification upgradableSpecification,
                                        ICustomFormatCalculationService formatService,
                                        IMediaFileService mediaFileService,
                                        Logger logger)
        {
            _upgradableSpecification = upgradableSpecification;
            _formatService = formatService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        // With update releases importing alongside the base (#149 phase 0), the
        // version on disk is the highest across ALL of the game's files, not
        // the primary's — comparing against the base alone would re-grab an
        // update that's already imported.
        private GameVersion GetHighestVersionOnDisk(RemoteGame subject, GameVersion primaryVersion)
        {
            var versions = (_mediaFileService.GetFilesByGame(subject.Game.Id) ?? new System.Collections.Generic.List<GameFile>())
                .Select(f => f.GameVersion)
                .Where(v => v?.HasValue == true)
                .ToList();

            if (primaryVersion?.HasValue == true)
            {
                versions.Add(primaryVersion);
            }

            return versions.OrderByDescending(v => v).FirstOrDefault();
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        private static string StripNonAlphanumeric(string value)
        {
            return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, SearchCriteriaBase searchCriteria)
        {
            var qualityProfile = subject.EffectiveQualityProfile;

            var contentType = subject.ParsedGameInfo?.ContentType ?? ReleaseContentType.Unknown;

            // A DLC-only release doesn't upgrade the base file — it fills its
            // own slot (#149). Comparing it against the base file's quality
            // either wrongly rejects it (cutoff met) or wrongly grabs it as a
            // base upgrade. Gate it on whether that DLC is already on disk.
            if (contentType is ReleaseContentType.DlcOnly or ReleaseContentType.SeasonPass)
            {
                var dlcFolders = (_mediaFileService.GetFilesByGame(subject.Game.Id) ?? new System.Collections.Generic.List<GameFile>())
                    .Where(f => f.RelativePath.IsNotNullOrWhiteSpace() &&
                                f.RelativePath.Replace('\\', '/').StartsWith("DLC/"))
                    .Select(f => StripNonAlphanumeric(f.RelativePath))
                    .ToList();

                var cleanParsedTitle = StripNonAlphanumeric(subject.ParsedGameInfo.PrimaryGameTitle);

                if (cleanParsedTitle.IsNotNullOrWhiteSpace() && dlcFolders.Any(f => f.Contains(cleanParsedTitle)))
                {
                    return DownloadSpecDecision.Reject(DownloadRejectionReason.DiskCutoffMet, "DLC is already on disk");
                }

                return DownloadSpecDecision.Accept();
            }

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
                var highestVersionOnDisk = GetHighestVersionOnDisk(subject, file.GameVersion);

                // Cutoff is met, but still check if it's a version upgrade
                if (_upgradableSpecification.IsVersionUpgrade(subject.Game, highestVersionOnDisk, subject.ParsedGameInfo.GameVersion))
                {
                    _logger.Debug(
                        "Cutoff met, but new release has newer game version {0} vs {1}, accepting",
                        subject.ParsedGameInfo.GameVersion,
                        highestVersionOnDisk);
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
                    if (_upgradableSpecification.IsVersionUpgrade(subject.Game, file.GameVersion, subject.ParsedGameInfo.GameVersion))
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
                    if (_upgradableSpecification.IsVersionUpgrade(subject.Game, file.GameVersion, subject.ParsedGameInfo.GameVersion))
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
                    if (_upgradableSpecification.IsVersionUpgrade(subject.Game, file.GameVersion, subject.ParsedGameInfo.GameVersion))
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
                    if (_upgradableSpecification.IsVersionUpgrade(subject.Game, file.GameVersion, subject.ParsedGameInfo.GameVersion))
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
                    if (_upgradableSpecification.IsVersionUpgrade(subject.Game, file.GameVersion, subject.ParsedGameInfo.GameVersion))
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
                    if (_upgradableSpecification.IsVersionUpgrade(subject.Game, file.GameVersion, subject.ParsedGameInfo.GameVersion))
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
