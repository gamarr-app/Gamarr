using System.IO;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles
{
    public static class GameFileExtensions
    {
        /// <summary>
        /// Gets the full path for a GameFile.
        /// When RelativePath is empty/null, the GameFile represents the game folder itself.
        /// </summary>
        public static string GetPath(this GameFile gameFile, Game game)
        {
            if (gameFile.RelativePath.IsNullOrWhiteSpace())
            {
                // Empty RelativePath means the game folder itself is the GameFile
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
        /// Returns true if this GameFile represents a folder (RelativePath is empty).
        /// </summary>
        public static bool IsFolder(this GameFile gameFile)
        {
            return gameFile.RelativePath.IsNullOrWhiteSpace();
        }
    }
}
