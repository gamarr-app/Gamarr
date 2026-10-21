using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    public class UpgradeSpecification : IImportDecisionEngineSpecification
    {
        private readonly IConfigService _configService;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly IUpgradableSpecification _upgradableSpecification;
        private readonly IMediaFileService _mediaFileService;
        private readonly IGameComponentService _componentService;
        private readonly Logger _logger;

        public UpgradeSpecification(IConfigService configService,
                                    ICustomFormatCalculationService formatService,
                                    IUpgradableSpecification upgradableSpecification,
                                    IMediaFileService mediaFileService,
                                    IGameComponentService componentService,
                                    Logger logger)
        {
            _configService = configService;
            _formatService = formatService;
            _upgradableSpecification = upgradableSpecification;
            _mediaFileService = mediaFileService;
            _componentService = componentService;
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var downloadPropersAndRepacks = _configService.DownloadPropersAndRepacks;
            var qualityProfile = localGame.Game.QualityProfile;
            var qualityComparer = new QualityModelComparer(qualityProfile);

            // A DLC-only, season-pass or update release does not replace the
            // base game — the mover imports it alongside into its own typed
            // subfolder (UpgradeMediaFileService, #149 phase 0). Falling through
            // to the base-file comparison below is what left these downloads at
            // importPending forever: a DLC folder parses as "Unknown v1" while
            // the base on disk is often a PROPER (v2), so every component
            // release read as a revision downgrade OF THE GAME (#204). Compare
            // against the file already in the slot this release would fill
            // instead, which is the import-side analog of the DlcOnly/SeasonPass
            // and UpdateOnly branches in UpgradeDiskSpecification (#203).
            if (localGame.IsComponentRelease)
            {
                return EvaluateComponentRelease(localGame, qualityComparer, downloadPropersAndRepacks);
            }

            if (localGame.Game.GameFileId > 0)
            {
                var gameFile = localGame.Game.GameFile;

                if (gameFile == null)
                {
                    _logger.Trace("Unable to get game file details from the DB. GameId: {0} GameFileId: {1}", localGame.Game.Id, localGame.Game.GameFileId);

                    return ImportSpecDecision.Accept();
                }

                var qualityCompare = qualityComparer.Compare(localGame.Quality.Quality, gameFile.Quality.Quality);

                // Get version info for version upgrade checks. The version on
                // disk is the highest across ALL of the game's files — update
                // releases import alongside the base (#149 phase 0), so the
                // primary's version alone would re-accept an already-imported
                // update.
                var newVersion = localGame.GameVersion;
                var existingVersions = (_mediaFileService.GetFilesByGame(localGame.Game.Id) ?? new System.Collections.Generic.List<GameFile>())
                    .Select(f => f.GameVersion)
                    .Where(v => v?.HasValue == true)
                    .ToList();

                if (gameFile.GameVersion?.HasValue == true)
                {
                    existingVersions.Add(gameFile.GameVersion);
                }

                var existingVersion = existingVersions.OrderByDescending(v => v).FirstOrDefault();

                if (qualityCompare < 0)
                {
                    // Quality is lower, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(localGame.Game, existingVersion, newVersion))
                    {
                        _logger.Debug("New file has newer game version {0} vs {1}, accepting despite lower quality", newVersion, existingVersion);
                    }
                    else
                    {
                        _logger.Debug("This file isn't a quality upgrade for game. Existing quality: {0}. New Quality {1}. Skipping {2}", CleanseLogMessage.SanitizeLogParam(gameFile.Quality.Quality?.ToString()), CleanseLogMessage.SanitizeLogParam(localGame.Quality.Quality?.ToString()), CleanseLogMessage.SanitizeLogParam(localGame.Path));
                        return ImportSpecDecision.Reject(ImportRejectionReason.NotQualityUpgrade, "Not an upgrade for existing game file. Existing quality: {0}. New Quality {1}.", gameFile.Quality.Quality, localGame.Quality.Quality);
                    }
                }

                // Same quality, propers/repacks are preferred and it is not a revision update. Reject revision downgrade.

                if (qualityCompare == 0 &&
                    downloadPropersAndRepacks != ProperDownloadTypes.DoNotPrefer &&
                    localGame.Quality.Revision.CompareTo(gameFile.Quality.Revision) < 0)
                {
                    // Revision is lower, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(localGame.Game, existingVersion, newVersion))
                    {
                        _logger.Debug("New file has newer game version {0} vs {1}, accepting despite lower revision", newVersion, existingVersion);
                    }
                    else
                    {
                        _logger.Debug("This file isn't a quality revision upgrade for game. Skipping {0}", localGame.Path);
                        return ImportSpecDecision.Reject(ImportRejectionReason.NotRevisionUpgrade, "Not a quality revision upgrade for existing game file(s)");
                    }
                }

                gameFile.Game = localGame.Game;
                var currentCustomFormats = _formatService.ParseCustomFormat(gameFile);
                var currentFormatScore = qualityProfile.CalculateCustomFormatScore(currentCustomFormats);
                var newCustomFormats = localGame.CustomFormats;
                var newFormatScore = localGame.CustomFormatScore;

                if (qualityCompare == 0 && newFormatScore < currentFormatScore)
                {
                    // Custom format score is lower, but check if it's a version upgrade
                    if (_upgradableSpecification.IsVersionUpgrade(localGame.Game, existingVersion, newVersion))
                    {
                        _logger.Debug("New file has newer game version {0} vs {1}, accepting despite lower custom format score", newVersion, existingVersion);
                    }
                    else
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
                }

                _logger.Debug("New item's custom formats [{0}] ({1}) do improve on [{2}] ({3}), accepting",
                    newCustomFormats != null ? newCustomFormats.ConcatToString() : "",
                    newFormatScore,
                    currentCustomFormats != null ? currentCustomFormats.ConcatToString() : "",
                    currentFormatScore);
            }

            return ImportSpecDecision.Accept();
        }

        private ImportSpecDecision EvaluateComponentRelease(LocalGame localGame, QualityModelComparer qualityComparer, ProperDownloadTypes downloadPropersAndRepacks)
        {
            var isDlc = localGame.ContentType is ReleaseContentType.DlcOnly or ReleaseContentType.SeasonPass;
            var componentType = isDlc ? GameComponentType.Dlc : GameComponentType.Update;

            var installed = isDlc
                ? GetInstalledDlcFile(localGame)
                : GetInstalledUpdateFile(localGame);

            // Nothing in the slot yet, so there is nothing to upgrade and
            // nothing to compare against. A bundle lands once and
            // GameComponentService links it to every slot it covers on the
            // GameFileAddedEvent that follows.
            if (installed == null)
            {
                _logger.Debug("No {0} component on disk matches {1}, accepting", componentType, CleanseLogMessage.SanitizeLogParam(localGame.Path));

                return ImportSpecDecision.Accept();
            }

            var qualityCompare = qualityComparer.Compare(localGame.Quality.Quality, installed.Quality.Quality);

            if (qualityCompare < 0)
            {
                _logger.Debug("This file isn't a quality upgrade for the installed {0} component. Existing quality: {1}. New Quality {2}. Skipping {3}",
                    componentType,
                    CleanseLogMessage.SanitizeLogParam(installed.Quality.Quality?.ToString()),
                    CleanseLogMessage.SanitizeLogParam(localGame.Quality.Quality?.ToString()),
                    CleanseLogMessage.SanitizeLogParam(localGame.Path));

                return ImportSpecDecision.Reject(ImportRejectionReason.NotQualityUpgrade, "Not an upgrade for the existing {0} component. Existing quality: {1}. New Quality {2}.", componentType, installed.Quality.Quality, localGame.Quality.Quality);
            }

            if (qualityCompare == 0 &&
                downloadPropersAndRepacks != ProperDownloadTypes.DoNotPrefer &&
                localGame.Quality.Revision.CompareTo(installed.Quality.Revision) < 0)
            {
                _logger.Debug("This file isn't a quality revision upgrade for the installed {0} component. Skipping {1}", componentType, CleanseLogMessage.SanitizeLogParam(localGame.Path));

                return ImportSpecDecision.Reject(ImportRejectionReason.NotRevisionUpgrade, "Not a quality revision upgrade for the existing {0} component", componentType);
            }

            return ImportSpecDecision.Accept();
        }

        /// <summary>
        /// The DLC file already imported for the DLC this release carries — not
        /// the base game file. DLC imports land in "DLC/&lt;release name&gt;"
        /// (UpgradeMediaFileService) and are linked to their component slot by
        /// GameComponentService on import, so match the slot first and fall back
        /// to the subfolder name for files an older database never linked.
        /// </summary>
        private GameFile GetInstalledDlcFile(LocalGame localGame)
        {
            var files = GetComponentFiles(localGame.Game.Id, "DLC/");

            if (!files.Any())
            {
                return null;
            }

            var releaseName = GetReleaseName(localGame);

            var matchingSlotIds = (_componentService.GetByGame(localGame.Game.Id) ?? new List<GameComponent>())
                .Where(c => c.Id > 0 &&
                            c.ComponentType == GameComponentType.Dlc &&
                            (GameComponentMatcher.ReleaseMatchesDlcTitle(releaseName, c.Title) ||
                             GameComponentMatcher.ReleaseMatchesDlcTitle(c.Title, releaseName)))
                .Select(c => c.Id)
                .ToHashSet();

            return files.FirstOrDefault(f => matchingSlotIds.Contains(f.ComponentId)) ??
                   files.FirstOrDefault(f => SubfolderMatches(f.RelativePath, "DLC/", releaseName));
        }

        /// <summary>
        /// The update file already imported for the same version. Update imports
        /// are keyed by their version ("Updates/v1.5"), so a different version is
        /// a different slot and has nothing on disk to upgrade.
        /// </summary>
        private GameFile GetInstalledUpdateFile(LocalGame localGame)
        {
            var files = GetComponentFiles(localGame.Game.Id, "Updates/");

            if (!files.Any())
            {
                return null;
            }

            var newVersion = localGame.GameVersion;

            // An unversioned update release can't be tied to a version slot, so
            // the folder name is the only identifier it has.
            if (newVersion?.HasValue != true)
            {
                return files.FirstOrDefault(f => SubfolderMatches(f.RelativePath, "Updates/", GetReleaseName(localGame)));
            }

            return files.FirstOrDefault(f => f.GameVersion?.HasValue == true && f.GameVersion.CompareTo(newVersion) == 0) ??
                   files.FirstOrDefault(f => GameVersion.Parse(GetSubfolder(f.RelativePath, "Updates/")).CompareTo(newVersion) == 0);
        }

        private List<GameFile> GetComponentFiles(int gameId, string prefix)
        {
            return (_mediaFileService.GetFilesByGame(gameId) ?? new List<GameFile>())
                .Where(f => f.Quality != null && GetSubfolder(f.RelativePath, prefix).IsNotNullOrWhiteSpace())
                .ToList();
        }

        // The release's own folder/file name, which is exactly what the mover
        // uses to name the component subfolder.
        private static string GetReleaseName(LocalGame localGame)
        {
            var name = Path.GetFileName(localGame.Path?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? string.Empty);

            return name.IsNotNullOrWhiteSpace() ? name : localGame.FileGameInfo?.ReleaseTitle;
        }

        // First path segment under the typed component folder, or empty when the
        // file isn't in one. A folder-style GameFile has an EMPTY RelativePath
        // (its path IS the game folder), so the null guard is load-bearing.
        private static string GetSubfolder(string relativePath, string prefix)
        {
            if (relativePath.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var normalized = relativePath.Replace('\\', '/');

            return normalized.StartsWith(prefix) ? normalized.Substring(prefix.Length).Split('/')[0] : string.Empty;
        }

        private static bool SubfolderMatches(string relativePath, string prefix, string releaseName)
        {
            var subfolder = StripNonAlphanumeric(GetSubfolder(relativePath, prefix));
            var clean = StripNonAlphanumeric(releaseName);

            return subfolder.IsNotNullOrWhiteSpace() &&
                   clean.IsNotNullOrWhiteSpace() &&
                   (subfolder.Contains(clean) || clean.Contains(subfolder));
        }

        private static string StripNonAlphanumeric(string value)
        {
            return new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }
    }
}
