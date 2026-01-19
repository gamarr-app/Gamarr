using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators
{
    public class AggregateReleaseInfo : IAggregateLocalGame
    {
        public int Order => 1;

        private readonly IHistoryService _historyService;

        public AggregateReleaseInfo(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        public LocalGame Aggregate(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            if (downloadClientItem == null)
            {
                return localGame;
            }

            var grabbedHistories = _historyService.FindByDownloadId(downloadClientItem.DownloadId)
                .Where(h => h.EventType == GameHistoryEventType.Grabbed)
                .ToList();

            if (grabbedHistories.Empty())
            {
                return localGame;
            }

            localGame.Release = new GrabbedReleaseInfo(grabbedHistories);

            return localGame;
        }
    }
}
