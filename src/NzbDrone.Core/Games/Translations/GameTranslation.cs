using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Games.Translations
{
    public class GameTranslation : Entity<GameTranslation>
    {
        public int GameMetadataId { get; set; }
        public string Title { get; set; }
        public string CleanTitle { get; set; }
        public string Overview { get; set; }
        public Language Language { get; set; }
    }
}
