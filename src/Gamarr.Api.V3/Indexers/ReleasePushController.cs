using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using Gamarr.Http;

namespace Gamarr.Api.V3.Indexers
{
    [V3ApiController("release/push")]
    public class ReleasePushController : ReleaseControllerBase
    {
        private readonly IMakeDownloadDecision _downloadDecisionMaker;
        private readonly IProcessDownloadDecisions _downloadDecisionProcessor;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IDownloadClientFactory _downloadClientFactory;
        private readonly Logger _logger;

        private static readonly object PushLock = new object();

        public ReleasePushController(IMakeDownloadDecision downloadDecisionMaker,
                                 IProcessDownloadDecisions downloadDecisionProcessor,
                                 IIndexerFactory indexerFactory,
                                 IDownloadClientFactory downloadClientFactory,
                                 IQualityProfileService qualityProfileService,
                                 Logger logger)
            : base(qualityProfileService)
        {
            _downloadDecisionMaker = downloadDecisionMaker;
            _downloadDecisionProcessor = downloadDecisionProcessor;
            _indexerFactory = indexerFactory;
            _downloadClientFactory = downloadClientFactory;
            _logger = logger;

            PostValidator.RuleFor(s => s.Title).NotEmpty();
            PostValidator.RuleFor(s => s.DownloadUrl).NotEmpty().When(s => s.MagnetUrl.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.MagnetUrl).NotEmpty().When(s => s.DownloadUrl.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.Protocol).NotEmpty();

            // ToModel only builds a TorrentInfo (the only carrier of MagnetUrl) for the torrent
            // protocol. A magnet-only release sent as usenet would validate here and then have its
            // magnet silently dropped, leaving UsenetClientBase to grab an empty DownloadUrl.
            PostValidator.RuleFor(s => s.Protocol)
                .Equal(DownloadProtocol.Torrent)
                .When(s => s.DownloadUrl.IsNullOrWhiteSpace() && s.MagnetUrl.IsNotNullOrWhiteSpace())
                .WithMessage("Must be 'torrent' when only a magnet url is supplied");
            PostValidator.RuleFor(s => s.PublishDate).NotEmpty();
        }

        [HttpPost]
        [Consumes("application/json")]
        public ActionResult<List<ReleaseResource>> Create([FromBody] ReleaseResource release)
        {
            _logger.Info("Release pushed: {0} - {1}", release.Title, release.DownloadUrl ?? release.MagnetUrl);

            ValidateResource(release);

            var info = release.ToModel();

            // A magnet-only release has no DownloadUrl, which would collapse every such push onto
            // the single guid "PUSH-".
            info.Guid = "PUSH-" + (info.DownloadUrl.IsNotNullOrWhiteSpace() ? info.DownloadUrl : release.MagnetUrl);

            ResolveIndexer(info);

            var downloadClientId = ResolveDownloadClientId(release);

            DownloadDecision decision;
            ProcessedDecisionResult grabResult;

            lock (PushLock)
            {
                var decisions = _downloadDecisionMaker.GetRssDecision(new List<ReleaseInfo> { info }, true);

                decision = decisions.FirstOrDefault();

                grabResult = _downloadDecisionProcessor.ProcessDecision(decision, downloadClientId).GetAwaiter().GetResult();
            }

            if (decision?.RemoteGame.ParsedGameInfo == null)
            {
                throw new ValidationException(new List<ValidationFailure> { new ("Title", "Unable to parse", release.Title) });
            }

            if (grabResult != ProcessedDecisionResult.Grabbed)
            {
                _logger.Warn("Pushed release '{0}' was not grabbed: {1}", release.Title, grabResult);
            }

            var resources = MapDecisions(new[] { decision });

            // Unlike every other endpoint that returns a ReleaseResource, this one performs the
            // grab inside the request, so it knows whether the grab worked. Reporting only
            // Approved would answer a question the caller did not ask: a release can pass every
            // specification and then fail to reach the download client, and until this was
            // returned the only trace of that was a log line.
            foreach (var resource in resources)
            {
                resource.GrabResult = grabResult;
            }

            return resources;
        }

        private void ResolveIndexer(ReleaseInfo release)
        {
            if (release.IndexerId == 0 && release.Indexer.IsNotNullOrWhiteSpace())
            {
                var indexer = _indexerFactory.All().FirstOrDefault(v => v.Name.EqualsIgnoreCase(release.Indexer));

                if (indexer != null)
                {
                    release.IndexerId = indexer.Id;
                    _logger.Debug("Push Release {0} associated with indexer {1} - {2}.", release.Title, release.IndexerId, release.Indexer);
                }
                else
                {
                    _logger.Debug("Push Release {0} not associated with known indexer {1}.", release.Title, release.Indexer);
                }
            }
            else if (release.IndexerId != 0 && release.Indexer.IsNullOrWhiteSpace())
            {
                try
                {
                    var indexer = _indexerFactory.Get(release.IndexerId);
                    release.Indexer = indexer.Name;
                    _logger.Debug("Push Release {0} associated with indexer {1} - {2}.", release.Title, release.IndexerId, release.Indexer);
                }
                catch (ModelNotFoundException)
                {
                    _logger.Debug("Push Release {0} not associated with known indexer {1}.", release.Title, release.IndexerId);
                    release.IndexerId = 0;
                }
            }
            else
            {
                _logger.Debug("Push Release {0} not associated with an indexer.", release.Title);
            }
        }

        private int? ResolveDownloadClientId(ReleaseResource release)
        {
            var downloadClientId = release.DownloadClientId.GetValueOrDefault();

            if (downloadClientId == 0 && release.DownloadClient.IsNotNullOrWhiteSpace())
            {
                var downloadClient = _downloadClientFactory.All().FirstOrDefault(v => v.Name.EqualsIgnoreCase(release.DownloadClient));

                if (downloadClient != null)
                {
                    _logger.Debug("Push Release {0} associated with download client {1} - {2}.", release.Title, downloadClientId, release.DownloadClient);

                    return downloadClient.Id;
                }

                _logger.Debug("Push Release {0} not associated with known download client {1}.", release.Title, release.DownloadClient);
            }

            return release.DownloadClientId;
        }
    }
}
