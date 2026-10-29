using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using SharpCompress.Archives;

namespace NzbDrone.Core.MediaFiles.Archives
{
    public interface IGameArchiveService
    {
        /// <param name="path">The import unit: a loose file or a release folder.</param>
        /// <param name="allowWholeArchive">
        /// When true, an archive that holds no single extractable ROM entry is
        /// still reported — in whole-archive mode — instead of being refused.
        /// The caller owns that decision because it depends on the game's
        /// platform, which this service deliberately knows nothing about.
        /// </param>
        GameArchiveInspection InspectImportUnit(string path, bool allowWholeArchive = false);
        void ExtractEntry(GameArchiveInspection inspection, string destinationPath);
        void ExtractArchive(GameArchiveInspection inspection, string destinationFolder);
    }

    /// <summary>
    /// Narrow, game-focused archive handling for releases that wrap a single ROM
    /// in a .zip/.rar/.7z (a BlueRoms Switch release is a .7z around one .xci).
    /// Extracting on import is the only way those land as something a console
    /// library scanner can read.
    ///
    /// Two modes. Single-entry: exactly one ROM/disc image inside, extracted to
    /// a file. Whole-archive: no ROM entry at all, which is the ordinary shape of
    /// a PC release (exe, data files, subfolders) — the entire tree is unpacked
    /// into the game folder. The caller decides which is allowed, because the
    /// choice depends on the game's platform and this service never sees it.
    ///
    /// Deliberately separate from <c>NzbDrone.Common.IArchiveService</c>: that one
    /// is the updater/backup tool — ZIP + tar.gz only, and it runs a full-archive
    /// integrity test over every byte, which is fatal on a multi-gigabyte release.
    /// </summary>
    public class GameArchiveService : IGameArchiveService
    {
        /// <summary>
        /// Container formats we will look inside. Deliberately short.
        /// </summary>
        public static readonly HashSet<string> ContainerExtensions = new (StringComparer.OrdinalIgnoreCase)
        {
            ".7z",
            ".rar",
            ".zip"
        };

        /// <summary>
        /// Entry formats worth extracting: console ROM/disc images that are a
        /// single self-contained file. PC payloads (.iso, .exe, .msi, .bin/.cue,
        /// repack .dat) are excluded on purpose — those archives hold a whole
        /// installer tree that must not be collapsed into one file.
        /// </summary>
        public static readonly HashSet<string> ExtractableEntryExtensions = new (StringComparer.OrdinalIgnoreCase)
        {
            // Nintendo Switch — the case this was built for; ownfoil only reads these three
            ".nsp",
            ".nsz",
            ".xci",

            // Handhelds / older Nintendo
            ".3ds",
            ".cia",
            ".dsi",
            ".fds",
            ".gb",
            ".gba",
            ".gbc",
            ".nds",
            ".nes",
            ".sfc",
            ".smc",

            // Nintendo 64 / GameCube / Wii
            ".ciso",
            ".gcz",
            ".n64",
            ".rvz",
            ".v64",
            ".wbfs",
            ".z64",

            // Sega / Atari / NEC / SNK / Bandai
            ".32x",
            ".a26",
            ".a52",
            ".a78",
            ".chd",
            ".gen",
            ".gg",
            ".lnx",
            ".md",
            ".ngc",
            ".ngp",
            ".pce",
            ".sg",
            ".sms",
            ".ws",
            ".wsc"
        };

        // Volume members of a split set: name.part1.rar / name.7z.001 / name.zip.001
        // / name.r00 / name.z01. NotMultiPartSpecification catches .part1.rar but
        // nothing else here, and MediaFileExtensions.SplitVolumeRegex actively
        // makes ".001" look like a game file extension. v1 refuses all of them
        // rather than half-handling a multi-volume extract.
        private static readonly Regex SplitVolumeRegex = new (
            @"(\.part\d+\.rar|\.(?:7z|zip|rar)\.\d{3}|\.r\d{2}|\.z\d{2})$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public GameArchiveService(IDiskProvider diskProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public static bool IsContainer(string path)
        {
            return path.IsNotNullOrWhiteSpace() && ContainerExtensions.Contains(Path.GetExtension(path));
        }

        /// <summary>
        /// Peek at an import unit — a loose file <b>or</b> a release folder — and
        /// report the single game file inside it when the release is nothing but
        /// an archive wrapper. Returns null when the unit should be imported
        /// as-is, which is the overwhelmingly common case.
        ///
        /// Header-only: for .7z/.zip/.rar the central directory is parsed and
        /// uncompressed entry sizes read without decompressing any payload.
        /// </summary>
        public GameArchiveInspection InspectImportUnit(string path, bool allowWholeArchive = false)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return null;
            }

            try
            {
                if (_diskProvider.FolderExists(path))
                {
                    var archive = FindSoleArchiveInFolder(path);

                    return archive == null ? null : Inspect(archive, allowWholeArchive);
                }

                return IsContainer(path) ? Inspect(path, allowWholeArchive) : null;
            }
            catch (Exception ex)
            {
                // A release that merely looks archive-shaped must never take the
                // whole import down; falling through leaves the status quo, and
                // NotArchiveSpecification rejects an uninspected archive.
                _logger.Debug(ex, "Unable to inspect {0} for archive-wrapped game files", path);

                return null;
            }
        }

        /// <summary>
        /// Extract the one inspected entry to <paramref name="destinationPath"/>.
        /// Writes to a ".partial~" sibling and renames once complete, so a failed
        /// multi-gigabyte extract is never adopted as a game file by the next
        /// disk scan.
        /// </summary>
        public void ExtractEntry(GameArchiveInspection inspection, string destinationPath)
        {
            var partialPath = destinationPath + ".partial~";

            if (_diskProvider.FileExists(partialPath))
            {
                _logger.Debug("Removing stale partial extraction {0}", partialPath);
                _diskProvider.DeleteFile(partialPath);
            }

            _logger.Info("Extracting {0} from {1} to {2}", inspection.EntryName, inspection.ArchivePath, destinationPath);

            try
            {
                using (var archive = ArchiveFactory.Open(inspection.ArchivePath))
                {
                    var entry = archive.Entries.FirstOrDefault(e => !e.IsDirectory && e.Key == inspection.EntryName);

                    if (entry == null)
                    {
                        throw new GameArchiveExtractionException($"Entry '{inspection.EntryName}' is no longer present in {inspection.ArchivePath}");
                    }

                    using (var entryStream = entry.OpenEntryStream())
                    using (var outputStream = _diskProvider.OpenWriteStream(partialPath))
                    {
                        entryStream.CopyTo(outputStream);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_diskProvider.FileExists(partialPath))
                {
                    _diskProvider.DeleteFile(partialPath);
                }

                if (ex is GameArchiveExtractionException)
                {
                    throw;
                }

                throw new GameArchiveExtractionException($"Failed to extract '{inspection.EntryName}' from {inspection.ArchivePath}: {ex.Message}", ex);
            }

            _diskProvider.MoveFile(partialPath, destinationPath);
        }

        /// <summary>
        /// Extract every entry of a whole-archive inspection into
        /// <paramref name="destinationFolder"/> — the game folder. Writes the
        /// tree into a ".partial~" sibling first and only then promotes it, so a
        /// failed multi-gigabyte extract is never adopted as a game folder by the
        /// next disk scan.
        /// </summary>
        public void ExtractArchive(GameArchiveInspection inspection, string destinationFolder)
        {
            var partialPath = destinationFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".partial~";

            if (_diskProvider.FolderExists(partialPath))
            {
                _logger.Debug("Removing stale partial extraction folder {0}", partialPath);
                _diskProvider.DeleteFolder(partialPath, true);
            }

            _logger.Info("Extracting all of {0} to {1}", inspection.ArchivePath, destinationFolder);

            var extracted = 0;

            try
            {
                _diskProvider.CreateFolder(partialPath);

                using (var archive = ArchiveFactory.Open(inspection.ArchivePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.IsDirectory || entry.Key.IsNullOrWhiteSpace())
                        {
                            continue;
                        }

                        var relativePath = GetSafeRelativePath(entry.Key);

                        if (relativePath == null)
                        {
                            // An entry key can carry "..", a leading "/" or a
                            // drive letter; writing it verbatim would land
                            // outside the game folder.
                            _logger.Warn("Skipping unsafe archive entry '{0}' in {1}", entry.Key, inspection.ArchivePath);
                            continue;
                        }

                        var targetPath = Path.Combine(partialPath, relativePath);
                        var targetFolder = Path.GetDirectoryName(targetPath);

                        if (targetFolder.IsNotNullOrWhiteSpace() && !_diskProvider.FolderExists(targetFolder))
                        {
                            _diskProvider.CreateFolder(targetFolder);
                        }

                        using (var entryStream = entry.OpenEntryStream())
                        using (var outputStream = _diskProvider.OpenWriteStream(targetPath))
                        {
                            entryStream.CopyTo(outputStream);
                        }

                        extracted++;
                    }
                }

                if (extracted == 0)
                {
                    throw new GameArchiveExtractionException($"No extractable entries remain in {inspection.ArchivePath}");
                }

                PromotePartialFolder(partialPath, destinationFolder);
            }
            catch (Exception ex)
            {
                CleanUpPartialFolder(partialPath);

                if (ex is GameArchiveExtractionException)
                {
                    throw;
                }

                throw new GameArchiveExtractionException($"Failed to extract {inspection.ArchivePath} to {destinationFolder}: {ex.Message}", ex);
            }

            _logger.Debug("Extracted {0} entries from {1} into {2}", extracted, inspection.ArchivePath, destinationFolder);
        }

        /// <summary>
        /// Move the completed partial tree into place. The game folder normally
        /// already exists (the mover created it), so the contents are moved in
        /// one level down rather than renaming the directory itself.
        /// </summary>
        private void PromotePartialFolder(string partialPath, string destinationFolder)
        {
            if (!_diskProvider.FolderExists(destinationFolder))
            {
                _diskProvider.MoveFolder(partialPath, destinationFolder);

                return;
            }

            foreach (var folder in _diskProvider.GetDirectories(partialPath).ToList())
            {
                var name = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                _diskProvider.MoveFolder(folder, Path.Combine(destinationFolder, name));
            }

            foreach (var file in _diskProvider.GetFiles(partialPath, false).ToList())
            {
                _diskProvider.MoveFile(file, Path.Combine(destinationFolder, Path.GetFileName(file)), true);
            }

            _diskProvider.DeleteFolder(partialPath, true);
        }

        private void CleanUpPartialFolder(string partialPath)
        {
            try
            {
                if (_diskProvider.FolderExists(partialPath))
                {
                    _diskProvider.DeleteFolder(partialPath, true);
                }
            }
            catch (Exception ex)
            {
                // Never mask the extraction failure with a cleanup failure.
                _logger.Warn(ex, "Unable to clean up partial extraction folder {0}", partialPath);
            }
        }

        /// <summary>
        /// Normalise an archive entry key to a path that cannot escape the
        /// destination, or null when it is not safe to write at all.
        /// </summary>
        private static string GetSafeRelativePath(string entryKey)
        {
            var normalized = entryKey.Replace('\\', '/').Trim();

            if (normalized.IsNullOrWhiteSpace() || normalized.StartsWith("/") || normalized.StartsWith("~/"))
            {
                return null;
            }

            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
            {
                return null;
            }

            foreach (var segment in segments)
            {
                // ".." escapes, "." is a no-op segment, and a colon is either a
                // drive letter or an NTFS alternate data stream.
                if (segment == "." || segment == ".." || segment.Contains(':'))
                {
                    return null;
                }
            }

            return Path.Combine(segments);
        }

        private string FindSoleArchiveInFolder(string folder)
        {
            var files = _diskProvider.GetFiles(folder, true).ToList();

            // Anything already importable on its own means the folder is not a
            // bare archive wrapper — leave it to the normal folder transfer.
            if (files.Any(f => !IsContainer(f) && MediaFileExtensions.IsGameFileExtension(Path.GetExtension(f))))
            {
                return null;
            }

            var archives = files.Where(IsContainer).ToList();

            if (archives.Count != 1)
            {
                if (archives.Count > 1)
                {
                    _logger.Debug("Found {0} archives in {1}, not treating it as an archive-wrapped release", archives.Count, folder);
                }

                return null;
            }

            return archives[0];
        }

        private GameArchiveInspection Inspect(string archivePath, bool allowWholeArchive)
        {
            if (SplitVolumeRegex.IsMatch(archivePath))
            {
                // Refused in both modes: half-handling a multi-volume extract is
                // worse than leaving the release alone.
                _logger.Debug("{0} is part of a split archive set, which is not supported", archivePath);

                return null;
            }

            using (var archive = ArchiveFactory.Open(archivePath))
            {
                var files = archive.Entries
                    .Where(e => !e.IsDirectory && e.Key.IsNotNullOrWhiteSpace())
                    .ToList();

                var candidates = files
                    .Where(e => ExtractableEntryExtensions.Contains(Path.GetExtension(e.Key)))
                    .ToList();

                if (candidates.Count != 1)
                {
                    // Zero ROM entries is the ordinary shape of a PC release: an
                    // exe, data files and subfolders, none of which can be
                    // collapsed into a single game file. Those games import at
                    // folder granularity anyway, so extract the whole tree.
                    // Several ROM entries stays refused — picking one would be a
                    // guess, and merging them is not a folder import.
                    if (candidates.Count == 0 && allowWholeArchive && files.Count > 0)
                    {
                        // Whole-archive extraction copies the tree verbatim into
                        // the game folder with no further per-file checks
                        // (NotExecutableSpecification never sees the individual
                        // entries — the LocalGame it evaluates points at the
                        // archive, not at what's inside it). An archive wrapping
                        // nothing but Windows installers would otherwise land in
                        // the library unexamined, so refuse the whole-archive
                        // path for that shape and fall through to the "holds N
                        // extractable game files, expected exactly 1" refusal
                        // below — NotArchiveSpecification then rejects the
                        // uninspected archive outright.
                        if (FileExtensions.IsInstallerOnlyPayload(files.Select(e => e.Key)))
                        {
                            _logger.Debug("{0} holds no single extractable game file and its entries are nothing but Windows installers; refusing whole-archive extraction", archivePath);

                            return null;
                        }

                        var totalSize = files.Sum(e => e.Size);

                        _logger.Debug("{0} holds no single extractable game file; extracting all {1} entries ({2} bytes uncompressed) as a game folder",
                            archivePath,
                            files.Count,
                            totalSize);

                        return new GameArchiveInspection
                        {
                            ArchivePath = archivePath,
                            EntryName = null,
                            EntrySize = totalSize,
                            ExtractWholeArchive = true
                        };
                    }

                    _logger.Debug("{0} holds {1} extractable game files, expected exactly 1", archivePath, candidates.Count);

                    return null;
                }

                var entry = candidates[0];

                _logger.Debug("{0} wraps a single game file: {1} ({2} bytes uncompressed)", archivePath, entry.Key, entry.Size);

                return new GameArchiveInspection
                {
                    ArchivePath = archivePath,
                    EntryName = entry.Key,
                    EntrySize = entry.Size
                };
            }
        }
    }
}
