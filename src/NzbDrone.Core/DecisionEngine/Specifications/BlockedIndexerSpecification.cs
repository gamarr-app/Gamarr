using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class BlockedIndexerSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly Logger _logger;

        private readonly ICachedDictionary<IndexerStatus> _blockedIndexerCache;

        public BlockedIndexerSpecification(IIndexerStatusService indexerStatusService, ICacheManager cacheManager, Logger logger)
        {
            _indexerStatusService = indexerStatusService;
            _logger = logger;

            _blockedIndexerCache = cacheManager.GetCacheDictionary(GetType(), "blocked", FetchBlockedIndexer, TimeSpan.FromSeconds(15));
        }

        public SpecificationPriority Priority => SpecificationPriority.Database;
        public RejectionType Type => RejectionType.Temporary;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteGame subject, ReleaseDecisionInformation information)
        {
            // This check exists because grabbing normally means fetching the .torrent/.nzb back
            // from the indexer, which a blocked indexer will not serve. A release that carries
            // only a magnet is handed straight to the download client and never touches the
            // indexer at all, so the indexer's failure state says nothing about whether the grab
            // can succeed. Rejecting it here breaks the manual-push recovery route at exactly the
            // moment anyone reaches for it: when the indexer is failing.
            if (subject.Release.DownloadUrl.IsNullOrWhiteSpace() &&
                subject.Release is TorrentInfo { MagnetUrl: var magnetUrl } &&
                magnetUrl.IsNotNullOrWhiteSpace())
            {
                _logger.Debug("Release '{0}' is magnet-only, so a blocked indexer cannot prevent the grab.", subject.Release.Title);

                return DownloadSpecDecision.Accept();
            }

            var status = _blockedIndexerCache.Find(subject.Release.IndexerId.ToString());
            if (status != null)
            {
                return DownloadSpecDecision.Reject(DownloadRejectionReason.IndexerDisabled, $"Indexer {subject.Release.Indexer} is blocked till {status.DisabledTill} due to failures, cannot grab release.");
            }

            return DownloadSpecDecision.Accept();
        }

        private IDictionary<string, IndexerStatus> FetchBlockedIndexer()
        {
            return _indexerStatusService.GetBlockedProviders().ToDictionary(v => v.ProviderId.ToString());
        }
    }
}
