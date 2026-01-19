using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Games
{
    public class MonitoringOptions : IEmbeddedDocument
    {
        public bool IgnoreEpisodesWithFiles { get; set; }
        public bool IgnoreEpisodesWithoutFiles { get; set; }
        public MonitorTypes Monitor { get; set; }
    }

    public enum MonitorTypes
    {
        GameOnly,
        GameAndCollection,
        None
    }
}
