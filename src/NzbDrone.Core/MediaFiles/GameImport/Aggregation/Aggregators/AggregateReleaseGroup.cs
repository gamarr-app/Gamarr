using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators
{
    public class AggregateReleaseGroup : IAggregateLocalGame
    {
        public int Order => 1;

        public LocalGame Aggregate(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var releaseGroup = localGame.DownloadClientGameInfo?.ReleaseGroup;

            if (releaseGroup.IsNullOrWhiteSpace())
            {
                releaseGroup = localGame.FolderGameInfo?.ReleaseGroup;
            }

            if (releaseGroup.IsNullOrWhiteSpace())
            {
                releaseGroup = localGame.FileGameInfo?.ReleaseGroup;
            }

            localGame.ReleaseGroup = releaseGroup;

            return localGame;
        }
    }
}
