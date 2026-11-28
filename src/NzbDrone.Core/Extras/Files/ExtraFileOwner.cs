using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Files
{
    /// <summary>
    /// Decides which GameFile an extra file (metadata, image, nfo, ...) belongs to.
    ///
    /// A game folder can hold several units: the base game plus "Updates/&lt;version&gt;"
    /// and "DLC/&lt;name&gt;" components. The existing-extra importers used to stamp the
    /// game's primary GameFileId onto every extra found anywhere under the folder, so
    /// extras that sat inside an update or DLC unit were attributed to the base game
    /// (which is also what made CleanupDuplicateMetadataFiles see false duplicates).
    ///
    /// Sonarr attributes an extra to the episode file whose parsed identity it shares;
    /// Radarr has one file per movie and simply uses the movie's file. Neither shape fits
    /// a multi-unit game, so the rule here is the path analogue of Sonarr's: the most
    /// specific unit whose path contains the extra owns it, and only an extra no
    /// component claims falls back to the base/primary file.
    /// </summary>
    public static class ExtraFileOwner
    {
        private static readonly Regex FileExtension = new Regex(@"^\.[A-Za-z][A-Za-z0-9]{1,4}$", RegexOptions.Compiled);

        /// <summary>
        /// The update/DLC unit that contains the extra, or null when no component does.
        /// Never falls back, so callers repairing existing rows can tell "belongs to a
        /// component" from "nothing more specific than the base".
        /// </summary>
        public static GameFile FindComponentOwner(IEnumerable<GameFile> gameFiles, string extraRelativePath)
        {
            if (gameFiles == null || string.IsNullOrWhiteSpace(extraRelativePath))
            {
                return null;
            }

            var extra = Normalize(extraRelativePath);
            var extraDir = ParentOf(extra);

            GameFile best = null;
            var bestScore = -1;

            foreach (var file in gameFiles)
            {
                if (file == null || !(GameFileUnit.IsUpdate(file) || GameFileUnit.IsDlc(file)))
                {
                    continue;
                }

                var score = Score(Normalize(file.RelativePath), extra, extraDir);

                if (score > bestScore)
                {
                    best = file;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>
        /// The GameFileId to record for an extra: the owning component when there is one,
        /// otherwise the game's base file. The primary pointer is used as-is when it is
        /// itself a base unit; if it points at a component (which an alongside import
        /// can leave behind) an extra that no component claimed is not that component's.
        /// </summary>
        public static int FindOwnerId(Game game, IEnumerable<GameFile> gameFiles, string extraRelativePath)
        {
            var files = gameFiles?.ToList() ?? new List<GameFile>();
            var owner = FindComponentOwner(files, extraRelativePath);

            if (owner != null)
            {
                return owner.Id;
            }

            var primary = files.FirstOrDefault(f => f.Id == game.GameFileId);

            if (primary == null || GameFileUnit.IsBase(primary))
            {
                return game.GameFileId;
            }

            return files.FirstOrDefault(GameFileUnit.IsBase)?.Id ?? game.GameFileId;
        }

        private static int Score(string unit, string extra, string extraDir)
        {
            // The unit is a folder holding the extra.
            if (extra.StartsWith(unit + "/", StringComparison.OrdinalIgnoreCase))
            {
                return unit.Length;
            }

            var unitDir = ParentOf(unit);

            // The unit is a file; extras next to it only count when the directory is the
            // unit's own (Updates/1.2/x.nsp), not the shared Updates/ or DLC/ root.
            if (unitDir.Contains('/') && string.Equals(unitDir, extraDir, StringComparison.OrdinalIgnoreCase))
            {
                return unitDir.Length;
            }

            // Same-name sidecar of a unit file directly under Updates/ or DLC/.
            var unitExtension = Path.GetExtension(unit);

            if (FileExtension.IsMatch(unitExtension) &&
                string.Equals(unitDir, extraDir, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(unit.Substring(0, unit.Length - unitExtension.Length),
                              extra.Substring(0, extra.Length - Path.GetExtension(extra).Length),
                              StringComparison.OrdinalIgnoreCase))
            {
                return unit.Length;
            }

            return -1;
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/').Trim('/');
        }

        private static string ParentOf(string path)
        {
            var index = path.LastIndexOf('/');

            return index < 0 ? string.Empty : path.Substring(0, index);
        }
    }
}
