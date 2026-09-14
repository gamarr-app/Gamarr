using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Messaging.Events;
using Gamarr.Http;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Queue
{
    [V3ApiController("queue")]
    public class QueueActionController : Controller
    {
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly IDownloadService _downloadService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public QueueActionController(IPendingReleaseService pendingReleaseService,
                                     IDownloadService downloadService,
                                     IEventAggregator eventAggregator,
                                     Logger logger)
        {
            _pendingReleaseService = pendingReleaseService;
            _downloadService = downloadService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        [HttpPost("grab/{id:int}")]
        public async Task<object> Grab([FromRoute] int id)
        {
            await GrabPendingRelease(id);

            return new { };
        }

        [HttpPost("grab/bulk")]
        [Consumes("application/json")]
        public async Task<object> Grab([FromBody] QueueBulkResource resource)
        {
            foreach (var id in resource.Ids)
            {
                await GrabPendingRelease(id);
            }

            return new { };
        }

        // This used to call DownloadReport with no handling at all, so a failed grab of a pending
        // release surfaced as a 500 and left no history row and nothing counting towards the
        // repeated-failure cap - the same gap the interactive grab had.
        private async Task GrabPendingRelease(int id)
        {
            var pendingRelease = _pendingReleaseService.FindPendingQueueItem(id);

            if (pendingRelease == null)
            {
                throw new NotFoundException();
            }

            try
            {
                await _downloadService.DownloadReport(pendingRelease.RemoteGame, null);
            }
            catch (IndexerBlockedException ex)
            {
                // Specific arm first: nothing was sent, so this is not a failed grab.
                _logger.Warn(ex.Message);

                throw new NzbDroneClientException(HttpStatusCode.Conflict, ex.Message);
            }
            catch (ReleaseDownloadException ex)
            {
                _logger.Error(ex, ex.Message);

                _eventAggregator.PublishEvent(new GameGrabFailedEvent(pendingRelease.RemoteGame, ex.Message));

                throw new NzbDroneClientException(HttpStatusCode.Conflict, ex.Message);
            }
        }
    }
}
