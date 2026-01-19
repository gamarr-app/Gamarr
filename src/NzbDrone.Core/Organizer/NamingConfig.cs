using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameGames = false,
            ReplaceIllegalCharacters = true,
            ColonReplacementFormat = ColonReplacementFormat.Smart,
            GameFolderFormat = "{Game Title} ({Release Year})",
            StandardGameFormat = "{Game Title} ({Release Year}) {Quality Full}",
        };

        public bool RenameGames { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public ColonReplacementFormat ColonReplacementFormat { get; set; }
        public string StandardGameFormat { get; set; }
        public string GameFolderFormat { get; set; }
    }
}
