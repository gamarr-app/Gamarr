using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.MediaFiles
{
    public static class FileExtensions
    {
        private static readonly Regex FileExtensionRegex = new (@"\.[a-z0-9]{2,4}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> UsenetExtensions = new HashSet<string>()
        {
            ".par2",
            ".nzb"
        };

        public static HashSet<string> ArchiveExtensions => new (StringComparer.OrdinalIgnoreCase)
        {
            ".7z",
            ".bz2",
            ".gz",
            ".r00",
            ".rar",
            ".tar.bz2",
            ".tar.gz",
            ".tar",
            ".tb2",
            ".tbz2",
            ".tgz",
            ".zip"
        };
        public static HashSet<string> DangerousExtensions => new (StringComparer.OrdinalIgnoreCase)
        {
            ".arj",
            ".lnk",
            ".lzh",
            ".ps1",
            ".scr",
            ".vbs",
            ".zipx"
        };
        public static HashSet<string> ExecutableExtensions => new (StringComparer.OrdinalIgnoreCase)
        {
            ".bat",
            ".cmd",
            ".exe",
            ".sh"
        };

        // Extensions the OS hands to an interpreter or loader the moment the
        // file is opened, whatever the name in front of them claims. Wider than
        // ExecutableExtensions (the narrow "never import this" list) because it
        // also covers script hosts and installer formats. Note that .exe and
        // .msi are legitimate game payloads in MediaFileExtensions — callers on
        // the import side must allow those through unless they are masquerading.
        public static HashSet<string> UnsafeExecutableExtensions => new (StringComparer.OrdinalIgnoreCase)
        {
            ".appx",
            ".bat",
            ".cmd",
            ".com",
            ".exe",
            ".jar",
            ".js",
            ".jse",
            ".msi",
            ".msix",
            ".pif",
            ".ps1",
            ".scr",
            ".vbe",
            ".vbs",
            ".wsf",
            ".wsh"
        };

        // Windows installer/package formats. .exe/.msi are legitimate game
        // payloads when they ship alongside real game data (a repack's
        // setup.exe next to its .bin files) — see IsInstallerOnlyPayload,
        // which is the only place these get treated as dangerous on their own.
        // .msix/.appx are folded in here too: they are never a recognised game
        // file extension (MediaFileExtensions), so a bare one is already
        // rejected elsewhere, but they get the same "installer, not data"
        // classification for this check.
        public static HashSet<string> WindowsInstallerExtensions => new (StringComparer.OrdinalIgnoreCase)
        {
            ".appx",
            ".exe",
            ".msi",
            ".msix"
        };

        // Files that carry no game content either way: a readme, a checksum
        // list, GOG's install metadata. A payload that is nothing but an
        // installer plus one of these is still "nothing but an installer" —
        // these don't count as the "real content" that would clear it.
        public static HashSet<string> IncidentalFileExtensions => new (StringComparer.OrdinalIgnoreCase)
        {
            ".bmp",
            ".cfg",
            ".diz",
            ".gif",
            ".ico",
            ".info",
            ".ini",
            ".jpg",
            ".jpeg",
            ".json",
            ".log",
            ".md5",
            ".nfo",
            ".pdf",
            ".png",
            ".rtf",
            ".sfv",
            ".txt",
            ".url"
        };

        // Content extensions that never legitimately sit in front of an
        // executable one. "Show.S01E01.mkv.exe" is a Windows binary wearing a
        // video file's name; only the LAST extension is what the OS honours.
        private static readonly HashSet<string> ContentExtensions = new (StringComparer.OrdinalIgnoreCase)
        {
            ".avi",
            ".flac",
            ".flv",
            ".jpg",
            ".m4v",
            ".mkv",
            ".mov",
            ".mp3",
            ".mp4",
            ".mpeg",
            ".mpg",
            ".pdf",
            ".png",
            ".txt",
            ".webm",
            ".wmv"
        };

        /// <summary>
        /// The extension the OS actually acts on: the last one, not the first.
        /// </summary>
        public static string GetEffectiveExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.GetExtension(path.Trim());
        }

        /// <summary>
        /// True when the effective extension is one the OS executes.
        /// </summary>
        public static bool IsUnsafeExecutable(string path)
        {
            return UnsafeExecutableExtensions.Contains(GetEffectiveExtension(path));
        }

        /// <summary>
        /// True when an executable extension is hidden behind a content one,
        /// e.g. "Movie.2024.1080p.mkv.exe" or "Game.Title.iso.scr".
        /// </summary>
        public static bool IsMasqueradedExecutable(string path)
        {
            if (!IsUnsafeExecutable(path))
            {
                return false;
            }

            var precedingExtension = GetEffectiveExtension(Path.GetFileNameWithoutExtension(path.Trim()));

            return ContentExtensions.Contains(precedingExtension) ||
                   MediaFileExtensions.IsGameFileExtension(precedingExtension);
        }

        /// <summary>
        /// True when a release's payload is nothing but Windows installer
        /// executables (.exe/.msi/.msix/.appx) plus incidental metadata
        /// (readme, checksum list, GOG's goggame-*.info, ...) — no actual game
        /// data. Used wherever the full set of paths in a candidate payload is
        /// already known (a folder's files, an archive's entries, or a
        /// single-file release treated as a one-element list) — never walks
        /// the disk itself.
        ///
        /// The payload is the only signal available for a release whose title
        /// carries no group token to check against: the 2026-10-26 incident
        /// this guards against was a trojan .msi blocklisted under the bare
        /// game title "Castlevania: Belmont's Curse", with no release group
        /// anywhere in it.
        /// </summary>
        public static bool IsInstallerOnlyPayload(IEnumerable<string> paths)
        {
            var extensions = paths.Select(GetEffectiveExtension).ToList();

            if (!extensions.Any(e => WindowsInstallerExtensions.Contains(e)))
            {
                return false;
            }

            var hasOtherContent = extensions.Any(e =>
                !WindowsInstallerExtensions.Contains(e) && !IncidentalFileExtensions.Contains(e));

            return !hasOtherContent;
        }

        public static string RemoveFileExtension(string title)
        {
            title = FileExtensionRegex.Replace(title, m =>
            {
                var extension = m.Value.ToLower();
                if (MediaFileExtensions.Extensions.Contains(extension) || UsenetExtensions.Contains(extension))
                {
                    return string.Empty;
                }

                return m.Value;
            });

            return title;
        }
    }
}
