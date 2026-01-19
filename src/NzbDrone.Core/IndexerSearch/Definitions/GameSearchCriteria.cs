namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class GameSearchCriteria : SearchCriteriaBase
    {
        public override string ToString()
        {
            return string.Format("[{0}]", Game.Title);
        }
    }
}
