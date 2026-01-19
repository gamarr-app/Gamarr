using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Aggregation.Aggregators
{
    public interface IAggregateRemoteGame
    {
        RemoteGame Aggregate(RemoteGame remoteGame);
    }
}
