using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public enum RenameProfile
    {
        Gamarr = 0,
        NoIntroPreserveById = 1,
        NoIntroCanonical = 2
    }

    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameGames = false,
            RenameProfile = RenameProfile.Gamarr,
            ReplaceIllegalCharacters = true,
            ColonReplacementFormat = ColonReplacementFormat.Smart,
            GameFolderFormat = "{Game Title} ({Release Year})",
            StandardGameFormat = "{Game Title} ({Release Year}) {Quality Full}",
        };

        public bool RenameGames { get; set; }
        public RenameProfile RenameProfile { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public ColonReplacementFormat ColonReplacementFormat { get; set; }
        public string StandardGameFormat { get; set; }
        public string GameFolderFormat { get; set; }
    }
}
