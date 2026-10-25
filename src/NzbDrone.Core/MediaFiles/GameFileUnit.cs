using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.MediaFiles
{
    /// <summary>
    /// Which part of a game a stored file is. The import layout puts updates
    /// under "Updates/&lt;version&gt;" and DLC under "DLC/&lt;name&gt;"; a
    /// folder-granularity release is the game folder itself and has an empty
    /// RelativePath, so anything that is neither an update nor a DLC is the
    /// base game.
    ///
    /// This used to be reimplemented in every caller that needed it, which is
    /// how "is the base game on disk?" ended up being answered by the game's
    /// GameFileId pointer in one place and by the files in another (#216).
    /// Keep the classification here.
    /// </summary>
    public static class GameFileUnit
    {
        public static bool IsUpdate(GameFile file)
        {
            return HasPrefix(file, "Updates/");
        }

        public static bool IsDlc(GameFile file)
        {
            return HasPrefix(file, "DLC/");
        }

        public static bool IsBase(GameFile file)
        {
            return file != null && !IsUpdate(file) && !IsDlc(file);
        }

        /// <summary>
        /// True when the game has a base game file recorded, whatever its
        /// primary GameFileId happens to point at. This is the question the
        /// decision engine actually wants to ask about an update/DLC release:
        /// the pointer can be 0 while a base file row already exists (mid-import)
        /// or point at a unit other than the base.
        /// </summary>
        public static bool HasBaseOnDisk(IEnumerable<GameFile> files)
        {
            return files?.Any(IsBase) == true;
        }

        private static bool HasPrefix(GameFile file, string prefix)
        {
            return file?.RelativePath.IsNotNullOrWhiteSpace() == true &&
                   file.RelativePath.Replace('\\', '/').StartsWith(prefix);
        }
    }
}
