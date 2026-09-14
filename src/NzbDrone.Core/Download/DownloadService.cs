using System;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public interface IDownloadService
    {
        Task DownloadReport(RemoteGame remoteGame, int? downloadClientId);
    }

    public class DownloadService : IDownloadService
    {
        // What a 429 with no Retry-After header is worth. HttpIndexerBase has always used an
        // hour for exactly this case on the search side; the grab side passing TimeSpan.Zero
        // through instead is why a grab-side rate limit never accumulated any back-off.
        private static readonly TimeSpan MinimumIndexerBackOff = TimeSpan.FromHours(1);

        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly IDownloadClientStatusService _downloadClientStatusService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ISeedConfigProvider _seedConfigProvider;
        private readonly Logger _logger;

        public DownloadService(IProvideDownloadClient downloadClientProvider,
                               IDownloadClientStatusService downloadClientStatusService,
                               IIndexerFactory indexerFactory,
                               IIndexerStatusService indexerStatusService,
                               IRateLimitService rateLimitService,
                               IEventAggregator eventAggregator,
                               ISeedConfigProvider seedConfigProvider,
                               Logger logger)
        {
            _downloadClientProvider = downloadClientProvider;
            _downloadClientStatusService = downloadClientStatusService;
            _indexerFactory = indexerFactory;
            _indexerStatusService = indexerStatusService;
            _rateLimitService = rateLimitService;
            _eventAggregator = eventAggregator;
            _seedConfigProvider = seedConfigProvider;
            _logger = logger;
        }

        public async Task DownloadReport(RemoteGame remoteGame, int? downloadClientId)
        {
            var filterBlockedClients = remoteGame.Release.PendingReleaseReason == PendingReleaseReason.DownloadClientUnavailable;

            var tags = remoteGame.Game?.Tags;

            var downloadClient = downloadClientId.HasValue
                ? _downloadClientProvider.Get(downloadClientId.Value)
                : _downloadClientProvider.GetDownloadClient(remoteGame.Release.DownloadProtocol, remoteGame.Release.IndexerId, filterBlockedClients, tags);

            await DownloadReport(remoteGame, downloadClient);
        }

        private async Task DownloadReport(RemoteGame remoteGame, IDownloadClient downloadClient)
        {
            Ensure.That(remoteGame.Game, () => remoteGame.Game).IsNotNull();

            var downloadTitle = remoteGame.Release.Title;

            if (downloadClient == null)
            {
                throw new DownloadClientUnavailableException($"{remoteGame.Release.DownloadProtocol} Download client isn't configured yet");
            }

            EnsureIndexerIsNotBlocked(remoteGame);

            // Get the seed configuration for this release.
            remoteGame.SeedConfiguration = _seedConfigProvider.GetSeedConfiguration(remoteGame);

            // Limit grabs to 2 per second.
            if (remoteGame.Release.DownloadUrl.IsNotNullOrWhiteSpace() && !remoteGame.Release.DownloadUrl.StartsWith("magnet:"))
            {
                var url = new HttpUri(remoteGame.Release.DownloadUrl);
                await _rateLimitService.WaitAndPulseAsync(url.Host, TimeSpan.FromSeconds(2));
            }

            IIndexer indexer = null;

            if (remoteGame.Release.IndexerId > 0)
            {
                indexer = _indexerFactory.GetInstance(_indexerFactory.Get(remoteGame.Release.IndexerId));
            }

            string downloadClientId;
            try
            {
                downloadClientId = await downloadClient.Download(remoteGame, indexer);
                _downloadClientStatusService.RecordSuccess(downloadClient.Definition.Id);
                _indexerStatusService.RecordSuccess(remoteGame.Release.IndexerId);
            }
            catch (ReleaseUnavailableException)
            {
                _logger.Trace("Release {0} no longer available on indexer.", remoteGame);
                throw;
            }
            catch (ReleaseBlockedException)
            {
                _logger.Trace("Release {0} previously added to blocklist, not sending to download client again.", remoteGame);
                throw;
            }
            catch (DownloadClientRejectedReleaseException)
            {
                _logger.Trace("Release {0} rejected by download client, possible duplicate.", remoteGame);
                throw;
            }
            catch (ReleaseDownloadException ex)
            {
                if (ex.InnerException is TooManyRequestsException http429)
                {
                    // A 429 is the indexer saying "stop asking", whether or not it bothered to
                    // attach a Retry-After. Prowlarr's grab-limit 429 carries none, so this used
                    // to record a plain escalating failure - and the very next successful search
                    // against the same indexer cleared it again through RecordSuccess. The
                    // back-off could therefore never survive a single RSS cycle.
                    var retryAfter = http429.RetryAfter != TimeSpan.Zero ? http429.RetryAfter : MinimumIndexerBackOff;

                    _logger.Warn("Grab limit reached for indexer {0}, backing off for {1}.", remoteGame.Release.Indexer, retryAfter);

                    _indexerStatusService.RecordFailure(remoteGame.Release.IndexerId, retryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(remoteGame.Release.IndexerId);
                }

                throw;
            }

            var gameGrabbedEvent = new GameGrabbedEvent(remoteGame);
            gameGrabbedEvent.DownloadClient = downloadClient.Name;
            gameGrabbedEvent.DownloadClientId = downloadClient.Definition.Id;
            gameGrabbedEvent.DownloadClientName = downloadClient.Definition.Name;

            if (downloadClientId.IsNotNullOrWhiteSpace())
            {
                gameGrabbedEvent.DownloadId = downloadClientId;
            }

            _logger.ProgressInfo("Report for {0} ({1}) sent to {2} from indexer {3}. {4}", remoteGame.Game.Title, remoteGame.Game.Year, downloadClient.Definition.Name, remoteGame.Release.Indexer, downloadTitle);
            _eventAggregator.PublishEvent(gameGrabbedEvent);
        }

        // Grabbing normally means fetching the .torrent/.nzb back from the indexer, so an indexer
        // that is already backed off after recent failures cannot serve one. Attempting it anyway
        // does not merely fail: the indexer re-arms its own rate limit from the attempt, so the
        // block never expires and every search in the instance keeps losing that indexer -
        // including searches for unrelated games. The search side has always filtered these out
        // (IndexerFactory.FilterBlockedIndexers); every grab entry point funnels through here, so
        // this is the one place that gives all of them the same treatment. In particular it also
        // covers the second and later releases of a single batch, whose decisions were made
        // before the first grab in that batch armed the block.
        private void EnsureIndexerIsNotBlocked(RemoteGame remoteGame)
        {
            var indexerId = remoteGame.Release.IndexerId;

            if (indexerId <= 0 || remoteGame.Release.IsMagnetOnly)
            {
                return;
            }

            var blockedIndexer = _indexerStatusService.GetBlockedProviders().FirstOrDefault(v => v.ProviderId == indexerId);

            if (blockedIndexer == null)
            {
                return;
            }

            var disabledTill = blockedIndexer.DisabledTill.Value.ToLocalTime();

            _logger.Warn("Not grabbing '{0}': indexer {1} is blocked till {2} due to recent failures.", remoteGame.Release.Title, remoteGame.Release.Indexer, disabledTill);

            throw new IndexerBlockedException(remoteGame.Release, $"Indexer {remoteGame.Release.Indexer} is blocked till {disabledTill} due to recent failures, not grabbing release");
        }
    }
}
