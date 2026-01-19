using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators
{
    public interface IAggregateLocalGame
    {
        int Order { get; }

        LocalGame Aggregate(LocalGame localGame, DownloadClientItem downloadClientItem);
    }
}
