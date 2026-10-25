using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.GameStats
{
    public class GameStatistics : ResultSet
    {
        public int GameId { get; set; }

        /// <summary>
        /// How many GameFiles rows the game owns — base plus every
        /// "Updates/&lt;ver&gt;" and "DLC/&lt;name&gt;" unit (#149). Counted from
        /// the file rows themselves; it used to be derived from the game's single
        /// GameFileId pointer and so was never anything but 0 or 1 (#221).
        /// </summary>
        public int GameFileCount { get; set; }

        /// <summary>
        /// 1 when Games.GameFileId names a file row, else 0. This is the primary
        /// (base) file POINTER, not a count of files and not a disk test (#216) —
        /// keep "does this game have its base file?" answers on this and not on
        /// <see cref="GameFileCount"/>, which a DLC-only game can make non-zero.
        /// </summary>
        public int PrimaryFileCount { get; set; }
        public long SizeOnDisk { get; set; }
        public string ReleaseGroupsString { get; set; }

        public List<string> ReleaseGroups
        {
            get
            {
                var releaseGroups = new List<string>();

                if (ReleaseGroupsString.IsNotNullOrWhiteSpace())
                {
                    releaseGroups = ReleaseGroupsString
                        .Split('|')
                        .Distinct()
                        .Where(rg => rg.IsNotNullOrWhiteSpace())
                        .OrderBy(rg => rg)
                        .ToList();
                }

                return releaseGroups;
            }
        }
    }
}
