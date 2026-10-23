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
        Expansion = 6,

        /// <summary>
        /// Kiosk / store demo, trial or preview build - a cut-down build that
        /// is neither the base game nor an add-on to it (#207)
        /// </summary>
        Demo = 7
    }

    public static class ReleaseContentTypeExtensions
    {
        /// <summary>
        /// Returns true if this release requires the base game to be installed
        /// </summary>
        /// <remarks>
        /// This is <see cref="IncludesDlc"/> minus <c>BaseGameWithAllDlc</c>
        /// (which carries its own base game) plus <c>UpdateOnly</c> (which is
        /// not DLC but is still applied on top of a base). Every member is
        /// accounted for deliberately:
        ///
        ///  * DlcOnly, SeasonPass, Expansion - add-on content, needs a base.
        ///  * UpdateOnly - a patch, needs a base.
        ///  * BaseGame, BaseGameWithAllDlc, Unknown - carry the base themselves.
        ///  * Demo - a kiosk build is not content applied on top of a base
        ///    game, it is a cut-down replacement for it. DemoBuildSpecification
        ///    rejects it outright instead.
        ///
        /// Expansion used to be false here, which meant both prefix paths in
        /// ParsingService bailed out before the DLC name latitude in
        /// IsAddonOfGameTitle could run, and a named expansion release never
        /// found its base game (#212). An expansion that is standalone and is
        /// its own library game still matches itself: the prefix paths try an
        /// exact title match first.
        /// </remarks>
        public static bool RequiresBaseGame(this ReleaseContentType type)
        {
            return type switch
            {
                ReleaseContentType.DlcOnly => true,
                ReleaseContentType.UpdateOnly => true,
                ReleaseContentType.SeasonPass => true,
                ReleaseContentType.Expansion => true,
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

        /// <summary>
        /// Returns true if this release fills a DLC component slot of its game
        /// rather than the base slot or an update slot: DLC-only, season passes
        /// and expansions.
        /// </summary>
        /// <remarks>
        /// This is DLC content that does NOT bring its own base game, which is
        /// what distinguishes a slot filler from a complete edition. Five call
        /// sites spelled the test out by hand as "DlcOnly or SeasonPass" - the
        /// import mover's DLC-vs-Updates subfolder choice, the import upgrade
        /// comparison, the component quality-profile lookup, the disk-cutoff
        /// check and the search filter - and each one would have had to be found
        /// separately when Expansion became base-requiring (#212). They now all
        /// ask here.
        /// </remarks>
        public static bool OccupiesDlcSlot(this ReleaseContentType type)
        {
            return type.IncludesDlc() && !type.IncludesBaseGame();
        }
    }
}
