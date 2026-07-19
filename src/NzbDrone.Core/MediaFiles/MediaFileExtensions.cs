using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public static class MediaFileExtensions
    {
        private static Dictionary<string, Quality> _fileExtensions;

        static MediaFileExtensions()
        {
            _fileExtensions = new Dictionary<string, Quality>(StringComparer.OrdinalIgnoreCase)
            {
                // Unknown/Generic
                { ".zip", Quality.Unknown },
                { ".rar", Quality.Unknown },
                { ".7z", Quality.Unknown },

                // ISO/Disc Images - typically retail or full releases
                { ".iso", Quality.ISO },
                { ".bin", Quality.ISO },
                { ".cue", Quality.ISO },
                { ".mdf", Quality.ISO },
                { ".mds", Quality.ISO },
                { ".nrg", Quality.ISO },
                { ".img", Quality.ISO },

                // Executables - typically scene or store releases
                { ".exe", Quality.Scene },
                { ".msi", Quality.Scene },

                // GOG installer format
                { ".gog", Quality.GOG },

                // DODI Repack format
                { ".doi", Quality.Repack },

                // FitGirl and other repack installer data
                { ".dat", Quality.Repack },
                { ".gb", Quality.Unknown },
                { ".gbc", Quality.Unknown },
                { ".gba", Quality.Unknown },
                { ".nds", Quality.Unknown },
                { ".dsi", Quality.Unknown },
                { ".3ds", Quality.Unknown },
                { ".cia", Quality.Unknown },
                { ".nes", Quality.Unknown },
                { ".fds", Quality.Unknown },
                { ".sfc", Quality.Unknown },
                { ".smc", Quality.Unknown },
                { ".n64", Quality.Unknown },
                { ".z64", Quality.Unknown },
                { ".v64", Quality.Unknown },
                { ".xci", Quality.Unknown },
                { ".nsp", Quality.Unknown },
                { ".nsz", Quality.Unknown },
                { ".rvz", Quality.ISO },
                { ".wbfs", Quality.ISO },
                { ".gcz", Quality.ISO },
                { ".ciso", Quality.ISO },
                { ".chd", Quality.ISO },
                { ".sms", Quality.Unknown },
                { ".gg", Quality.Unknown },
                { ".sg", Quality.Unknown },
                { ".md", Quality.Unknown },
                { ".gen", Quality.Unknown },
                { ".32x", Quality.Unknown },
                { ".pce", Quality.Unknown },
                { ".a26", Quality.Unknown },
                { ".a52", Quality.Unknown },
                { ".a78", Quality.Unknown },
                { ".lnx", Quality.Unknown },
                { ".ngp", Quality.Unknown },
                { ".ngc", Quality.Unknown },
                { ".ws", Quality.Unknown },
                { ".wsc", Quality.Unknown },
            };
        }

        // Split-volume archive parts: .001-.999 (7z/HJSplit), .r00-.r99
        // (old-style RAR sets), .z01-.z99 (split zip). These can't live in the
        // extension map: the parser strips trailing Extensions members from
        // release titles, which would eat version suffixes like "v1.001".
        private static readonly Regex SplitVolumeRegex = new (@"^\.(?:\d{3}|r\d{2}|z\d{2})$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static HashSet<string> Extensions => new HashSet<string>(_fileExtensions.Keys, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> DiskExtensions => new HashSet<string>(new[] { ".iso", ".bin", ".img", ".mdf", ".nrg" }, StringComparer.OrdinalIgnoreCase);

        public static bool IsGameFileExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            return _fileExtensions.ContainsKey(extension) || SplitVolumeRegex.IsMatch(extension);
        }

        public static Quality GetQualityForExtension(string extension)
        {
            if (_fileExtensions.TryGetValue(extension, out var quality))
            {
                return quality;
            }

            return Quality.Unknown;
        }
    }
}
