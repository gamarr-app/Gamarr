namespace NzbDrone.Core.Games
{
    /// <summary>
    /// Represents a gaming platform (console/PC/etc.)
    /// </summary>
    public enum GamePlatformCategory
    {
        Console = 1,
        Arcade = 2,
        Platform = 3,
        OperatingSystem = 4,
        PortableConsole = 5,
        Computer = 6
    }

    /// <summary>
    /// Common platform families for filtering
    /// </summary>
    public enum PlatformFamily
    {
        Unknown = 0,
        PC = 1,
        PlayStation = 2,
        Xbox = 3,
        Nintendo = 4,
        Sega = 5,
        Atari = 6,
        Mobile = 7,
        Linux = 8,
        Mac = 9
    }

    /// <summary>
    /// Represents a platform a game can be released on
    /// </summary>
    public class GamePlatform
    {
        public int IgdbId { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string Slug { get; set; }
        public PlatformFamily Family { get; set; }
        public GamePlatformCategory Category { get; set; }
        public int? Generation { get; set; }

        /// <summary>
        /// Maps IGDB platform family ID to our PlatformFamily enum
        /// </summary>
        public static PlatformFamily MapPlatformFamily(int? igdbFamilyId)
        {
            return igdbFamilyId switch
            {
                1 => PlatformFamily.PlayStation,
                2 => PlatformFamily.Xbox,
                3 => PlatformFamily.Sega,
                4 => PlatformFamily.Nintendo,
                5 => PlatformFamily.Atari,
                _ => PlatformFamily.Unknown
            };
        }

        /// <summary>
        /// Common IGDB Platform IDs for reference
        /// </summary>
        public static class CommonPlatforms
        {
            public const int Windows = 6;
            public const int Linux = 3;
            public const int Mac = 14;
            public const int PS5 = 167;
            public const int PS4 = 48;
            public const int PS3 = 9;
            public const int PSVita = 46;
            public const int XboxSeriesX = 169;
            public const int XboxOne = 49;
            public const int Xbox360 = 12;
            public const int Switch = 130;
            public const int WiiU = 41;
            public const int Nintendo3DS = 37;
        }
    }
}
