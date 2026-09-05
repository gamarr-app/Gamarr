using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDiskScanService
    {
        void Scan(Game game);
        string[] GetVideoFiles(string path, bool allDirectories = true);
        string[] GetNonVideoFiles(string path, bool allDirectories = true);
        List<string> FilterPaths(string basePath, IEnumerable<string> paths, bool filterExtras = true);
    }

    public class DiskScanService :
        IDiskScanService,
        IExecute<RescanGameCommand>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedGame _importApprovedGames;
        private readonly IConfigService _configService;
        private readonly IGameService _gameService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMediaFileTableCleanupService _mediaFileTableCleanupService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Games.Components.IGameComponentRepository _componentRepository;
        private readonly Logger _logger;

        public DiskScanService(IDiskProvider diskProvider,
                               IMakeImportDecision importDecisionMaker,
                               IImportApprovedGame importApprovedGames,
                               IConfigService configService,
                               IGameService gameService,
                               IMediaFileService mediaFileService,
                               IMediaFileTableCleanupService mediaFileTableCleanupService,
                               IRootFolderService rootFolderService,
                               IEventAggregator eventAggregator,
                               Games.Components.IGameComponentRepository componentRepository,
                               Logger logger)
        {
            _diskProvider = diskProvider;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedGames = importApprovedGames;
            _configService = configService;
            _gameService = gameService;
            _mediaFileService = mediaFileService;
            _mediaFileTableCleanupService = mediaFileTableCleanupService;
            _rootFolderService = rootFolderService;
            _eventAggregator = eventAggregator;
            _componentRepository = componentRepository;
            _logger = logger;
        }

        // "extras" and "other" cover bonus-content folders in GOG-style installers,
        // sample folders come with scene releases, and extrafanart is the Kodi
        // artwork-dump convention. The movie-era patterns (trailers, deleted
        // scenes, featurettes, "-scene." file suffixes) are gone — a game file
        // like cut-scene.pak must not be skipped.
        private static readonly Regex ExcludedExtrasSubFolderRegex = new Regex(@"(?:\\|\/|^)(?:extras|extrafanart|other|sample[s]?)(?:\\|\/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExcludedSubFoldersRegex = new Regex(@"(?:\\|\/|^)(?:@eadir|\.@__thumb|plex versions|\.[^\\/]+)(?:\\|\/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExcludedFilesRegex = new Regex(@"^\.(_|unmanic|DS_Store$)|^Thumbs\.db$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public void Scan(Game game)
        {
            var rootFolder = _rootFolderService.GetBestRootFolderPath(game.Path);

            var gameFolderExists = _diskProvider.FolderExists(game.Path);

            if (!gameFolderExists)
            {
                if (!_diskProvider.FolderExists(rootFolder))
                {
                    _logger.Warn("Game's root folder ({0}) doesn't exist.", rootFolder);
                    _eventAggregator.PublishEvent(new GameScanSkippedEvent(game, GameScanSkippedReason.RootFolderDoesNotExist));
                    return;
                }

                if (_diskProvider.FolderEmpty(rootFolder))
                {
                    _logger.Warn("Game's root folder ({0}) is empty. Rescan will not update games as a failsafe.", rootFolder);
                    _eventAggregator.PublishEvent(new GameScanSkippedEvent(game, GameScanSkippedReason.RootFolderIsEmpty));
                    return;
                }
            }

            _logger.ProgressInfo("Scanning disk for {0}", game.Title);

            if (!gameFolderExists)
            {
                if (_configService.CreateEmptyGameFolders)
                {
                    if (_configService.DeleteEmptyFolders)
                    {
                        _logger.Debug("Not creating missing game folder: {0} because delete empty game folders is enabled", game.Path);
                    }
                    else
                    {
                        _logger.Debug("Creating missing game folder: {0}", game.Path);

                        _diskProvider.CreateFolder(game.Path);
                        SetPermissions(game.Path);
                    }
                }
                else
                {
                    _logger.Debug("Game's folder doesn't exist: {0}", game.Path);
                }

                CleanMediaFiles(game, new List<string>());
                CompletedScanning(game, new List<string>());

                return;
            }

            // Check if the game folder has any content
            var filesInFolder = _diskProvider.GetFiles(game.Path, true);
            var folderHasContent = filesInFolder.Any();

            // Get existing game files from database
            var existingGameFiles = _mediaFileService.GetFilesByGame(game.Id);

            if (folderHasContent)
            {
                NormalizeNestedUpdateFolders(game);
                SplitBundledDlcFolders(game);
                AdoptUntrackedComponentFolders(game, existingGameFiles);

                // Subfolder units (update releases imported alongside the base,
                // RelativePath like Updates/<version>) are reconciled by their
                // own folder's existence and must not be absorbed or deleted by
                // the base-file logic below.
                var subfolderUnits = existingGameFiles
                    .Where(f => f.RelativePath.IsNotNullOrWhiteSpace() &&
                                _diskProvider.FolderExists(Path.Combine(game.Path, f.RelativePath)))
                    .ToList();

                var vanishedSubfolderUnits = existingGameFiles
                    .Where(f => f.RelativePath.IsNotNullOrWhiteSpace() &&
                                !_diskProvider.FolderExists(Path.Combine(game.Path, f.RelativePath)) &&
                                !_diskProvider.FileExists(Path.Combine(game.Path, f.RelativePath)))
                    .ToList();

                foreach (var vanished in vanishedSubfolderUnits)
                {
                    _logger.Debug("Removing GameFile record for missing path: {0}", vanished.RelativePath);
                    _mediaFileService.Delete(vanished, DeleteMediaFileReason.MissingFromDisk);
                }

                existingGameFiles = existingGameFiles
                    .Except(subfolderUnits)
                    .Except(vanishedSubfolderUnits)
                    .ToList();

                // Console titles ship as one packaged file per unit, so the
                // folder-level collapse below has to be skipped for them —
                // see ShouldTrackPerFile. Fall back to the folder record when
                // nothing on disk looks like a game file, so a console folder
                // holding only unrecognised content keeps a record at all.
                var perFileCandidates = ShouldTrackPerFile(game)
                    ? FilterPaths(game.Path, GetVideoFiles(game.Path))
                        .Where(file => !IsUnderSubfolderUnit(subfolderUnits, game.Path.GetRelativePath(file)))
                        .ToList()
                    : new List<string>();

                if (perFileCandidates.Any())
                {
                    ReconcilePerFileRecords(game, existingGameFiles, subfolderUnits, perFileCandidates);
                }
                else
                {
                    // Folder has content - treat the entire folder as a single GameFile
                    var folderSize = _diskProvider.GetFolderSize(game.Path);

                    // Check if we already have a folder-based GameFile (RelativePath is empty)
                    var existingFolderFile = existingGameFiles.FirstOrDefault(f => f.RelativePath.IsNullOrWhiteSpace());

                    if (existingFolderFile != null)
                    {
                        // Update existing folder record if size changed
                        if (existingFolderFile.Size != folderSize)
                        {
                            _logger.Debug("Updating folder size for {0}: {1} -> {2}", game.Title, existingFolderFile.Size, folderSize);
                            existingFolderFile.Size = folderSize;
                            _mediaFileService.Update(existingFolderFile);
                        }
                    }
                    else
                    {
                        // No folder-based GameFile exists - migrate from file-based
                        // Preserve metadata from the first existing file (if any)
                        var sourceFile = existingGameFiles.FirstOrDefault();

                        // Delete old file-based GameFile records
                        foreach (var oldFile in existingGameFiles)
                        {
                            _logger.Debug("Removing old file-based GameFile record: {0}", oldFile.RelativePath);
                            _mediaFileService.Delete(oldFile, DeleteMediaFileReason.ManualOverride);
                        }

                        // Create new folder-based GameFile, preserving metadata from old record
                        _logger.Debug("Creating folder-based GameFile for {0}", game.Title);
                        var folderGameFile = new GameFile
                        {
                            GameId = game.Id,
                            RelativePath = string.Empty, // Empty means the folder itself
                            Size = folderSize,
                            DateAdded = sourceFile?.DateAdded ?? DateTime.UtcNow,
                            Quality = sourceFile?.Quality ?? new QualityModel { Quality = Quality.Unknown },
                            Languages = sourceFile?.Languages ?? new List<Language> { Language.Unknown },
                            IndexerFlags = sourceFile?.IndexerFlags ?? 0,
                            ReleaseGroup = sourceFile?.ReleaseGroup,
                            SceneName = sourceFile?.SceneName,
                            GameVersion = sourceFile?.GameVersion
                        };

                        _mediaFileService.Add(folderGameFile);
                    }
                }
            }
            else
            {
                // Folder is empty - clean up any existing GameFile records
                foreach (var existingFile in existingGameFiles)
                {
                    _logger.Debug("Removing GameFile for empty folder: {0}", existingFile.RelativePath);
                    _mediaFileService.Delete(existingFile, DeleteMediaFileReason.MissingFromDisk);
                }
            }

            var filesOnDisk = GetNonVideoFiles(game.Path);
            var possibleExtraFiles = FilterPaths(game.Path, filesOnDisk);

            RemoveEmptyGameFolder(game.Path);
            CompletedScanning(game, possibleExtraFiles);
        }

        // Granularity switch: a PC release is an *installation* — a
        // repack is dozens of .rar/.bin parts and an installed GOG game is
        // thousands of .dll/.pak files, none of which is meaningful on its
        // own — so the whole folder is tracked as one GameFile. A console
        // release is the opposite: one packaged file (.nsp/.xci/.iso/.rom) IS
        // one game, and a base sitting next to its update must stay two
        // records with two sizes rather than collapse into one row whose size
        // is the sum.
        //
        // This gates on the entry's platform rather than on sniffing
        // extensions, matching how PlatformSpecification / NoIntroCatalogDefaults
        // reason about consoles. It reuses the very same family primitives
        // GamePlatform.PlatformMatches is built from, so an entry stored as
        // plain Nintendo behaves like one stored as NintendoSwitch here
        // exactly as it already does in release filtering — PlatformMatches
        // itself is the wrong shape (it compares a wanted release platform
        // against an actual one, not "is this a console").
        // Note the codebase only ships IsNintendoFamily
        // and IsPlayStationFamily helpers — Xbox, Sega and Atari each collapse
        // to a single PlatformFamily value (MapPlatformFamily sends Xbox/360/
        // One/Series X all to PlatformFamily.Xbox), so they are named directly.
        // Unknown (the default for every pre-#150 entry) stays folder-based.
        private static bool ShouldTrackPerFile(Game game)
        {
            var platform = game.Platform;

            return GamePlatform.IsNintendoFamily(platform) ||
                   GamePlatform.IsPlayStationFamily(platform) ||
                   platform is PlatformFamily.Xbox or PlatformFamily.Sega or PlatformFamily.Atari;
        }

        // Files inside a tracked component folder (Updates/<version>,
        // DLC/<name>) belong to that unit's record and must not get their own.
        private static bool IsUnderSubfolderUnit(List<GameFile> subfolderUnits, string relativePath)
        {
            if (relativePath.IsNullOrWhiteSpace())
            {
                return false;
            }

            var normalized = relativePath.Replace('\\', '/');

            return subfolderUnits.Any(unit => normalized.StartsWith(
                unit.RelativePath.Replace('\\', '/').TrimEnd('/') + "/",
                StringComparison.OrdinalIgnoreCase));
        }

        private static bool RelativePathMatches(string left, string right)
        {
            return left.Replace('\\', '/').Equals(right.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        // One GameFile per packaged file, reconciled against disk. Records for
        // files that have vanished were already removed by the shared
        // missing-path sweep in Scan; there is deliberately no blanket delete
        // of file-based records here — that is precisely what would destroy a
        // correct per-file record produced by download import.
        private void ReconcilePerFileRecords(Game game, List<GameFile> existingGameFiles, List<GameFile> subfolderUnits, List<string> filesOnDisk)
        {
            var records = existingGameFiles
                .Where(f => !IsUnderSubfolderUnit(subfolderUnits, f.RelativePath))
                .ToList();

            var folderRecords = records.Where(f => f.RelativePath.IsNullOrWhiteSpace()).ToList();
            var metadataSource = folderRecords.FirstOrDefault() ?? records.FirstOrDefault();

            // Largest first: a console update/DLC package is always a fraction
            // of the base's size, so this puts the base package first. That
            // ordering is load-bearing — the folder record is recycled into the
            // first file and GameService assigns Game.GameFileId to the first
            // record added, so the game's primary file ends up being the base.
            var onDisk = filesOnDisk
                .Select(path => new
                {
                    RelativePath = game.Path.GetRelativePath(path),
                    Size = _diskProvider.GetFileSize(path),
                    Extension = Path.GetExtension(path)
                })
                .OrderByDescending(f => f.Size)
                .ThenBy(f => f.RelativePath, StringComparer.Ordinal)
                .ToList();

            var reconciled = new List<GameFile>();

            foreach (var file in onDisk)
            {
                var existing = records.FirstOrDefault(r => r.RelativePath.IsNotNullOrWhiteSpace() &&
                                                           RelativePathMatches(r.RelativePath, file.RelativePath) &&
                                                           !reconciled.Contains(r));

                if (existing != null)
                {
                    if (existing.Size != file.Size)
                    {
                        _logger.Debug("Updating size for {0}: {1} -> {2}", existing.RelativePath, existing.Size, file.Size);
                        existing.Size = file.Size;
                        _mediaFileService.Update(existing);
                    }

                    reconciled.Add(existing);
                    continue;
                }

                var recycled = folderRecords.FirstOrDefault();

                if (recycled != null)
                {
                    // Explode a folder-level record into per-file records by
                    // repurposing the row rather than delete-then-add: the row
                    // id is what Game.GameFileId points at, and it carries the
                    // metadata of the release that produced it.
                    folderRecords.Remove(recycled);

                    _logger.Debug("Exploding folder-level GameFile for {0} into per-file record: {1}", game.Title, file.RelativePath);
                    recycled.RelativePath = file.RelativePath;
                    recycled.Size = file.Size;
                    _mediaFileService.Update(recycled);

                    reconciled.Add(recycled);
                    continue;
                }

                _logger.Debug("Adding per-file GameFile for {0}: {1}", game.Title, file.RelativePath);

                var added = _mediaFileService.Add(new GameFile
                {
                    GameId = game.Id,
                    RelativePath = file.RelativePath,
                    Size = file.Size,
                    DateAdded = metadataSource?.DateAdded ?? DateTime.UtcNow,
                    Quality = new QualityModel { Quality = MediaFileExtensions.GetQualityForExtension(file.Extension) },
                    Languages = metadataSource?.Languages ?? new List<Language> { Language.Unknown },
                    IndexerFlags = metadataSource?.IndexerFlags ?? 0,
                    ReleaseGroup = metadataSource?.ReleaseGroup
                });

                reconciled.Add(added ?? new GameFile { GameId = game.Id, RelativePath = file.RelativePath });
            }

            // A leftover folder record can only happen if a game somehow has
            // more than one; it would otherwise sit there claiming the summed
            // size of the whole folder.
            foreach (var leftover in folderRecords)
            {
                _logger.Debug("Removing surplus folder-level GameFile record for {0}", game.Title);
                _mediaFileService.Delete(leftover, DeleteMediaFileReason.ManualOverride);
            }

            EnsurePrimaryFile(game, reconciled, subfolderUnits);
        }

        // With N records per game, Game.GameFileId can be left dangling when
        // the row it named is the one whose file vanished. Re-point it at the
        // base record (first reconciled = largest package) instead of leaving
        // the game looking file-less while its files are on disk. A pointer
        // that still names a tracked record is left alone.
        private void EnsurePrimaryFile(Game game, List<GameFile> reconciled, List<GameFile> subfolderUnits)
        {
            var baseFile = reconciled.FirstOrDefault();

            if (baseFile == null || baseFile.Id == 0)
            {
                return;
            }

            if (game.GameFileId != 0 &&
                (reconciled.Any(f => f.Id == game.GameFileId) || subfolderUnits.Any(f => f.Id == game.GameFileId)))
            {
                return;
            }

            _logger.Debug("Pointing {0} at base file record [{1}] {2}", game.Title, baseFile.Id, baseFile.RelativePath);
            game.GameFileId = baseFile.Id;
            _gameService.UpdateGame(game);
        }

        // Some releases ship the base game with separate update packages
        // inside ("game.iso" + "update_1.7.1/update.iso"). After such a
        // release imports as the base, those folders sit at the game root;
        // move them into the canonical Updates/<version> layout so they become
        // their own components (#149). Only VERSIONED update-style names are
        // touched — a bare "patch"/"DLC" folder is often integral game content
        // and must not be relocated.
        private static readonly Regex NestedUpdateFolderRegex = new (@"^(update|patch|hotfix)e?s?(?=[\s._-]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private void NormalizeNestedUpdateFolders(Game game)
        {
            foreach (var dir in _diskProvider.GetDirectories(game.Path))
            {
                var name = Path.GetFileName(dir);

                if (!NestedUpdateFolderRegex.IsMatch(name))
                {
                    continue;
                }

                // Pad: the version regexes require a leading delimiter, which a bare
                // folder name like "update_1.7.1" lacks.
                var version = QualityParser.ParseGameVersion($" {name} ");

                if (version?.HasValue != true)
                {
                    continue;
                }

                var destination = Path.Combine(game.Path, "Updates", version.ToString().Replace(' ', '-'));

                if (_diskProvider.FolderExists(destination))
                {
                    continue;
                }

                _diskProvider.CreateFolder(Path.Combine(game.Path, "Updates"));
                _diskProvider.MoveFolder(dir, destination);
                _logger.Info("Moved nested update package '{0}' to '{1}'", name, destination);
            }
        }

        // Bundled DLC splitting (#149): a release can ship the base plus
        // separately-packaged DLC ("game.iso" + "Beach.Pack.DLC/dlc.iso").
        // Splitting is only safe when BOTH hold:
        //  - the folder name matches a metadata-confirmed DLC slot (IGDB or
        //    Steam), so we know it IS that DLC and not game content, and
        //  - the folder contains a packaged payload (disc image / archive)
        //    rather than loose files — GOG-style installers lay integral DLC
        //    data into the game tree, and those folders must stay put.
        private static readonly string[] DlcPayloadExtensions = { ".iso", ".rar", ".zip", ".7z", ".bin", ".nrg", ".mds" };

        private void SplitBundledDlcFolders(Game game)
        {
            var dlcSlots = _componentRepository.GetByGame(game.Id)
                .Where(c => c.ComponentType == Games.Components.GameComponentType.Dlc &&
                            (c.Key.StartsWith("igdb:") || c.Key.StartsWith("steam:")))
                .ToList();

            if (!dlcSlots.Any())
            {
                return;
            }

            foreach (var dir in _diskProvider.GetDirectories(game.Path))
            {
                var name = Path.GetFileName(dir);

                if (name.Equals("Updates", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("DLC", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var slot = dlcSlots.FirstOrDefault(s => Games.Components.GameComponentMatcher.ReleaseMatchesDlcTitle(name, s.Title));

                if (slot == null)
                {
                    continue;
                }

                var hasPackagedPayload = _diskProvider.GetFiles(dir, false)
                    .Any(f => DlcPayloadExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));

                if (!hasPackagedPayload)
                {
                    _logger.Debug("Folder '{0}' matches DLC '{1}' but has no packaged payload — leaving in place as game content", name, slot.Title);
                    continue;
                }

                var destination = Path.Combine(game.Path, "DLC", name);

                if (_diskProvider.FolderExists(destination))
                {
                    continue;
                }

                try
                {
                    _diskProvider.CreateFolder(Path.Combine(game.Path, "DLC"));
                    _diskProvider.MoveFolder(dir, destination);
                    _logger.Info("Split bundled DLC '{0}' (matches '{1}') into '{2}'", name, slot.Title, destination);
                }
                catch (Exception ex)
                {
                    // A locked or partially-written folder must not abort the whole scan;
                    // leave it in place and carry on with the remaining folders.
                    _logger.Warn(ex, "Failed to split bundled DLC '{0}' into '{1}' — leaving in place", name, destination);
                }
            }
        }

        // Canonical component subfolders (Updates/<version>, DLC/<name>) that
        // exist on disk without a GameFile record — from the normalization
        // above or dropped in manually — get adopted as tracked units.
        private void AdoptUntrackedComponentFolders(Game game, List<GameFile> existingGameFiles)
        {
            foreach (var container in new[] { "Updates", "DLC" })
            {
                var containerPath = Path.Combine(game.Path, container);

                if (!_diskProvider.FolderExists(containerPath))
                {
                    continue;
                }

                foreach (var dir in _diskProvider.GetDirectories(containerPath))
                {
                    var relativePath = Path.Combine(container, Path.GetFileName(dir));

                    // PathEquals rejects non-rooted paths, so relative paths
                    // must be compared as normalized strings (Sentry 7620897427:
                    // the throw aborted every rescan of a game with tracked
                    // DLC/ files).
                    var normalized = relativePath.Replace('\\', '/');

                    if (existingGameFiles.Any(f => f.RelativePath.IsNotNullOrWhiteSpace() &&
                                                   f.RelativePath.Replace('\\', '/').Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var adopted = new GameFile
                    {
                        GameId = game.Id,
                        RelativePath = relativePath,
                        Size = _diskProvider.GetFolderSize(dir),
                        DateAdded = DateTime.UtcNow,
                        Quality = new QualityModel { Quality = Quality.Unknown },
                        Languages = new List<Language> { Language.Unknown },
                        GameVersion = container == "Updates" ? QualityParser.ParseGameVersion($" {Path.GetFileName(dir)} ") : null
                    };

                    _logger.Info("Adopting untracked component folder as {0}", relativePath);
                    _mediaFileService.Add(adopted);
                    existingGameFiles.Add(adopted);
                }
            }
        }

        private void CleanMediaFiles(Game game, List<string> mediaFileList)
        {
            _logger.Debug("{0} Cleaning up media files in DB", game);
            _mediaFileTableCleanupService.Clean(game, mediaFileList);
        }

        private void CompletedScanning(Game game, List<string> possibleExtraFiles)
        {
            _logger.Info("Completed scanning disk for {0}", game.Title);
            _eventAggregator.PublishEvent(new GameScannedEvent(game, possibleExtraFiles));
        }

        public string[] GetVideoFiles(string path, bool allDirectories = true)
        {
            _logger.Debug("Scanning '{0}' for game files", CleanseLogMessage.SanitizeLogParam(path));

            var filesOnDisk = _diskProvider.GetFiles(path, allDirectories).ToList();

            var mediaFileList = filesOnDisk.Where(file => MediaFileExtensions.IsGameFileExtension(Path.GetExtension(file)))
                                           .ToList();

            _logger.Trace("{0} files were found in {1}", filesOnDisk.Count, CleanseLogMessage.SanitizeLogParam(path));
            _logger.Debug("{0} game files were found in {1}", mediaFileList.Count, CleanseLogMessage.SanitizeLogParam(path));

            return mediaFileList.ToArray();
        }

        public string[] GetNonVideoFiles(string path, bool allDirectories = true)
        {
            _logger.Debug("Scanning '{0}' for non-game files", CleanseLogMessage.SanitizeLogParam(path));

            var filesOnDisk = _diskProvider.GetFiles(path, allDirectories).ToList();

            var mediaFileList = filesOnDisk.Where(file => !MediaFileExtensions.IsGameFileExtension(Path.GetExtension(file)))
                                           .ToList();

            _logger.Trace("{0} files were found in {1}", filesOnDisk.Count, CleanseLogMessage.SanitizeLogParam(path));
            _logger.Debug("{0} non-game files were found in {1}", mediaFileList.Count, CleanseLogMessage.SanitizeLogParam(path));

            return mediaFileList.ToArray();
        }

        public List<string> FilterPaths(string basePath, IEnumerable<string> paths, bool filterExtras = true)
        {
            var filteredPaths =  paths.Where(path => !ExcludedSubFoldersRegex.IsMatch(basePath.GetRelativePath(path)))
                                      .Where(path => !ExcludedFilesRegex.IsMatch(Path.GetFileName(path)))
                                      .ToList();

            if (filterExtras)
            {
                filteredPaths = filteredPaths.Where(path => !ExcludedExtrasSubFolderRegex.IsMatch(basePath.GetRelativePath(path)))
                                             .ToList();
            }

            return filteredPaths;
        }

        private void SetPermissions(string path)
        {
            if (!_configService.SetPermissionsLinux)
            {
                return;
            }

            try
            {
                _diskProvider.SetPermissions(path, _configService.ChmodFolder, _configService.ChownGroup);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to apply permissions to: " + path);
                _logger.Debug(ex, ex.Message);
            }
        }

        private void RemoveEmptyGameFolder(string path)
        {
            if (_configService.DeleteEmptyFolders)
            {
                _diskProvider.RemoveEmptySubfolders(path);

                if (_diskProvider.FolderEmpty(path))
                {
                    _diskProvider.DeleteFolder(path, true);
                }
            }
        }

        public void Execute(RescanGameCommand message)
        {
            if (message.GameId.HasValue)
            {
                var game = _gameService.GetGame(message.GameId.Value);
                Scan(game);
            }
            else
            {
                var allGames = _gameService.GetAllGames();

                foreach (var game in allGames)
                {
                    Scan(game);
                }
            }
        }
    }
}
