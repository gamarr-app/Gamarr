namespace NzbDrone.Core.Notifications.Trakt.Resource
{
    public class TraktGameResource
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public TraktGameIdsResource Ids { get; set; }
    }
}
