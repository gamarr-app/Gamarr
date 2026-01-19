using System;
using System.Collections.Generic;
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

                // Common game archive formats used by repacks
                { ".bin.001", Quality.Repack },
                { ".part01.rar", Quality.Repack },

                // DODI Repack format
                { ".doi", Quality.Repack },

                // FitGirl and other repack installer data
                { ".dat", Quality.Repack },
            };
        }

        public static HashSet<string> Extensions => new HashSet<string>(_fileExtensions.Keys, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> DiskExtensions => new HashSet<string>(new[] { ".iso", ".bin", ".img", ".mdf", ".nrg" }, StringComparer.OrdinalIgnoreCase);

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
