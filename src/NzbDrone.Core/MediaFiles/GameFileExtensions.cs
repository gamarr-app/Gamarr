using System;
using System.IO;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles
{
    public static class GameFileExtensions
    {
        /// <summary>
        /// The value RelativePath carries when the game folder itself IS the
        /// game file (PC/unknown-platform releases, and whole-archive extracts):
        /// the folder's own name.
        /// </summary>
        /// <remarks>
        /// This used to be an empty string, which is a trap for every consumer —
        /// rename, organise, notifications and the UI all end up showing or
        /// building something from nothing. Storing the folder name instead
        /// gives them a real token to work with and matches what manual import
        /// already reports for a folder that IS the root (ManualImportService).
        /// </remarks>
        public static string GetFolderRelativePath(this Game game)
        {
            if (game?.Path.IsNullOrWhiteSpace() != false)
            {
                return string.Empty;
            }

            return Path.GetFileName(game.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        /// <summary>
        /// Gets the full path for a GameFile.
        /// When the GameFile represents the game folder itself, that is the game folder.
        /// </summary>
        public static string GetPath(this GameFile gameFile, Game game)
        {
            if (gameFile.IsFolder(game))
            {
                return game.Path;
            }

            return Path.Combine(game.Path, gameFile.RelativePath);
        }

        /// <summary>
        /// Gets the full path for a GameFile using the Game already attached to it.
        /// </summary>
        public static string GetPath(this GameFile gameFile)
        {
            return gameFile.GetPath(gameFile.Game);
        }

        /// <summary>
        /// Returns true if this GameFile represents the game folder itself
        /// rather than a file inside it.
        /// </summary>
        /// <remarks>
        /// Both shapes have to be recognised forever: records written before the
        /// folder name landed store an empty string, and no migration rewrites
        /// them (an old database reaches a new image long before anyone migrates
        /// anything). A subfolder unit — Updates/&lt;version&gt;, DLC/&lt;name&gt; — never
        /// collides because it always carries a separator.
        /// </remarks>
        public static bool IsFolder(this GameFile gameFile, Game game)
        {
            if (gameFile.RelativePath.IsNullOrWhiteSpace())
            {
                return true;
            }

            var folderName = game.GetFolderRelativePath();

            if (folderName.IsNullOrWhiteSpace())
            {
                return false;
            }

            return gameFile.RelativePath
                .Replace('\\', '/')
                .TrimEnd('/')
                .Equals(folderName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if this GameFile represents a folder, using the Game
        /// already attached to it.
        /// </summary>
        public static bool IsFolder(this GameFile gameFile)
        {
            return gameFile.IsFolder(gameFile.Game);
        }

        /// <summary>
        /// Returns true if this GameFile is a unit living in a subfolder of the
        /// game folder — an update or DLC imported as a folder, so
        /// Updates/&lt;version&gt; or DLC/&lt;name&gt;.
        /// </summary>
        /// <remarks>
        /// These records are neither the game folder nor a plain file next to
        /// it: their path is a directory, which is what made deleting them a
        /// silent no-op (gamarr-app/Gamarr#215). The separator is the only
        /// reliable marker, since IsFolder deliberately matches the game
        /// folder's own name and nothing else.
        /// </remarks>
        public static bool IsSubfolderUnit(this GameFile gameFile, Game game)
        {
            if (gameFile.RelativePath.IsNullOrWhiteSpace() || gameFile.IsFolder(game))
            {
                return false;
            }

            return gameFile.RelativePath
                .Replace('\\', '/')
                .Trim('/')
                .Contains('/');
        }
    }
}
