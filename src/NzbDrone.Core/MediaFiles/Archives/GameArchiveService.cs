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
        GameArchiveInspection InspectImportUnit(string path);
        void ExtractEntry(GameArchiveInspection inspection, string destinationPath);
    }

    /// <summary>
    /// Narrow, game-focused archive handling for releases that wrap a single ROM
    /// in a .zip/.rar/.7z (a BlueRoms Switch release is a .7z around one .xci).
    /// Extracting on import is the only way those land as something a console
    /// library scanner can read.
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
        public GameArchiveInspection InspectImportUnit(string path)
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

                    return archive == null ? null : Inspect(archive);
                }

                return IsContainer(path) ? Inspect(path) : null;
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

        private GameArchiveInspection Inspect(string archivePath)
        {
            if (SplitVolumeRegex.IsMatch(archivePath))
            {
                _logger.Debug("{0} is part of a split archive set, which is not supported", archivePath);

                return null;
            }

            using (var archive = ArchiveFactory.Open(archivePath))
            {
                var candidates = archive.Entries
                    .Where(e => !e.IsDirectory &&
                                e.Key.IsNotNullOrWhiteSpace() &&
                                ExtractableEntryExtensions.Contains(Path.GetExtension(e.Key)))
                    .ToList();

                if (candidates.Count != 1)
                {
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
