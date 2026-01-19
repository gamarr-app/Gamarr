namespace NzbDrone.Core.Games
{
    /// <summary>
    /// IGDB Game Category - Type of game content
    /// https://api-docs.igdb.com/#game-enums
    /// </summary>
    public enum GameType
    {
        MainGame = 0,
        DlcAddon = 1,
        Expansion = 2,
        Bundle = 3,
        StandaloneExpansion = 4,
        Mod = 5,
        Episode = 6,
        Season = 7,
        Remake = 8,
        Remaster = 9,
        ExpandedGame = 10,
        Port = 11,
        Fork = 12,
        Pack = 13,
        Update = 14
    }

    /// <summary>
    /// Extension methods for GameType
    /// </summary>
    public static class GameTypeExtensions
    {
        /// <summary>
        /// Returns true if this game type is downloadable content (DLC, addon, expansion)
        /// </summary>
        public static bool IsDlc(this GameType type)
        {
            return type switch
            {
                GameType.DlcAddon => true,
                GameType.Expansion => true,
                GameType.StandaloneExpansion => true,
                GameType.Episode => true,
                GameType.Season => true,
                GameType.Pack => true,
                _ => false
            };
        }

        /// <summary>
        /// Returns true if this is a main/standalone game
        /// </summary>
        public static bool IsMainGame(this GameType type)
        {
            return type switch
            {
                GameType.MainGame => true,
                GameType.StandaloneExpansion => true,
                GameType.Remake => true,
                GameType.Remaster => true,
                GameType.ExpandedGame => true,
                GameType.Port => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets a user-friendly display name for the game type
        /// </summary>
        public static string GetDisplayName(this GameType type)
        {
            return type switch
            {
                GameType.MainGame => "Game",
                GameType.DlcAddon => "DLC",
                GameType.Expansion => "Expansion",
                GameType.Bundle => "Bundle",
                GameType.StandaloneExpansion => "Standalone Expansion",
                GameType.Mod => "Mod",
                GameType.Episode => "Episode",
                GameType.Season => "Season",
                GameType.Remake => "Remake",
                GameType.Remaster => "Remaster",
                GameType.ExpandedGame => "Expanded Edition",
                GameType.Port => "Port",
                GameType.Fork => "Fork",
                GameType.Pack => "Pack",
                GameType.Update => "Update",
                _ => "Unknown"
            };
        }
    }
}
