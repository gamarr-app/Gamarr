using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Games.AlternativeTitles
{
    public class AlternativeTitle : Entity<AlternativeTitle>
    {
        public SourceType SourceType { get; set; }
        public int GameMetadataId { get; set; }
        public string Title { get; set; }
        public string CleanTitle { get; set; }

        public AlternativeTitle()
        {
        }

        public AlternativeTitle(string title, SourceType sourceType = SourceType.Igdb)
        {
            Title = title;
            CleanTitle = title.CleanGameTitle();
            SourceType = sourceType;
        }

        public override string ToString()
        {
            return $"{Title} [{CleanTitle}]";
        }
    }

    public enum SourceType
    {
        Igdb = 0,
        Mappings = 1,
        User = 2,
        Indexer = 3
    }

    public class AlternativeYear
    {
        public int Year { get; set; }
        public int SourceId { get; set; }
    }
}
