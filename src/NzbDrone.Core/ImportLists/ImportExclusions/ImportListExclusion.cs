using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.ImportLists.ImportExclusions
{
    public class ImportListExclusion : ModelBase
    {
        public int IgdbId { get; set; }
        public string GameTitle { get; set; }
        public int GameYear { get; set; }

        public new string ToString()
        {
            return string.Format("Excluded Game: [{0}][{1} {2}]", IgdbId, GameTitle, GameYear);
        }
    }
}
