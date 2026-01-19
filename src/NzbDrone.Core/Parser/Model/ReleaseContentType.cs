namespace NzbDrone.Core.Parser.Model
{
    /// <summary>
    /// Indicates the type of content in a game release
    /// </summary>
    public enum ReleaseContentType
    {
        /// <summary>
        /// Unknown or undetected content type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Base game only, no DLC included
        /// </summary>
        BaseGame = 1,

        /// <summary>
        /// Base game with all DLC included (Complete Edition, GOTY, etc.)
        /// </summary>
        BaseGameWithAllDlc = 2,

        /// <summary>
        /// DLC only, requires base game
        /// </summary>
        DlcOnly = 3,

        /// <summary>
        /// Update or patch only, requires base game
        /// </summary>
        UpdateOnly = 4,

        /// <summary>
        /// Season pass or DLC bundle
        /// </summary>
        SeasonPass = 5,

        /// <summary>
        /// Expansion pack (may or may not be standalone)
        /// </summary>
        Expansion = 6
    }

    public static class ReleaseContentTypeExtensions
    {
        /// <summary>
        /// Returns true if this release requires the base game to be installed
        /// </summary>
        public static bool RequiresBaseGame(this ReleaseContentType type)
        {
            return type switch
            {
                ReleaseContentType.DlcOnly => true,
                ReleaseContentType.UpdateOnly => true,
                ReleaseContentType.SeasonPass => true,
                _ => false
            };
        }

        /// <summary>
        /// Returns true if this release includes the base game
        /// </summary>
        public static bool IncludesBaseGame(this ReleaseContentType type)
        {
            return type switch
            {
                ReleaseContentType.BaseGame => true,
                ReleaseContentType.BaseGameWithAllDlc => true,
                ReleaseContentType.Unknown => true, // Assume base game if unknown
                _ => false
            };
        }

        /// <summary>
        /// Returns true if this release includes DLC content
        /// </summary>
        public static bool IncludesDlc(this ReleaseContentType type)
        {
            return type switch
            {
                ReleaseContentType.BaseGameWithAllDlc => true,
                ReleaseContentType.DlcOnly => true,
                ReleaseContentType.SeasonPass => true,
                ReleaseContentType.Expansion => true,
                _ => false
            };
        }
    }
}
