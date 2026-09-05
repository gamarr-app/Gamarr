using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.RootFolders
{
    /// <summary>
    /// The root folder a game of a given platform lands in by default. This is
    /// only a default: <see cref="Game.RootFolderPath"/> stays a per-entry
    /// choice made at add time, exactly like Radarr, and the user can override
    /// it in the add dialog. The row for <see cref="PlatformFamily.Unknown"/>
    /// doubles as the global default for platforms with no entry of their own.
    /// </summary>
    public class PlatformRootFolder : ModelBase
    {
        public PlatformFamily Platform { get; set; }
        public string Path { get; set; }
    }
}
