using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Crypto;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.Download.Pending
{
    public interface IPendingReleaseService
    {
        void Add(DownloadDecision decision, PendingReleaseReason reason);
        void AddMany(List<Tuple<DownloadDecision, PendingReleaseReason>> decisions);
        List<ReleaseInfo> GetPending();
        List<RemoteGame> GetPendingRemoteGames(int gameId);
        List<Queue.Queue> GetPendingQueue();
        Queue.Queue FindPendingQueueItem(int queueId);
        void RemovePendingQueueItems(int queueId);
        RemoteGame OldestPendingRelease(int gameId);
    }

    public class PendingReleaseService : IPendingReleaseService,
                                         IHandle<GameGrabbedEvent>,
                                         IHandle<GamesDeletedEvent>,
                                         IHandle<RssSyncCompleteEvent>
    {
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IPendingReleaseRepository _repository;
        private readonly IGameService _gameService;
        private readonly IParsingService _parsingService;
        private readonly IDelayProfileService _delayProfileService;
        private readonly ITaskManager _taskManager;
        private readonly IConfigService _configService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IRemoteGameAggregationService _aggregationService;
        private readonly IDownloadClientFactory _downloadClientFactory;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public PendingReleaseService(IIndexerStatusService indexerStatusService,
                                     IPendingReleaseRepository repository,
                                     IGameService gameService,
                                     IParsingService parsingService,
                                     IDelayProfileService delayProfileService,
                                     ITaskManager taskManager,
                                     IConfigService configService,
                                     ICustomFormatCalculationService formatCalculator,
                                     IRemoteGameAggregationService aggregationService,
                                     IDownloadClientFactory downloadClientFactory,
                                     IIndexerFactory indexerFactory,
                                     IEventAggregator eventAggregator,
                                     Logger logger)
        {
            _indexerStatusService = indexerStatusService;
            _repository = repository;
            _gameService = gameService;
            _parsingService = parsingService;
            _delayProfileService = delayProfileService;
            _taskManager = taskManager;
            _configService = configService;
            _formatCalculator = formatCalculator;
            _aggregationService = aggregationService;
            _downloadClientFactory = downloadClientFactory;
            _indexerFactory = indexerFactory;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void Add(DownloadDecision decision, PendingReleaseReason reason)
        {
            AddMany(new List<Tuple<DownloadDecision, PendingReleaseReason>> { Tuple.Create(decision, reason) });
        }

        public void AddMany(List<Tuple<DownloadDecision, PendingReleaseReason>> decisions)
        {
            foreach (var gameDecisions in decisions.GroupBy(v => v.Item1.RemoteGame.Game.Id))
            {
                var game = gameDecisions.First().Item1.RemoteGame.Game;
                var alreadyPending = _repository.AllByGameId(game.Id);

                foreach (var pair in gameDecisions)
                {
                    var decision = pair.Item1;
                    var reason = pair.Item2;

                    var existingReports = alreadyPending ?? Enumerable.Empty<PendingRelease>();

                    var matchingReports = existingReports.Where(MatchingReleasePredicate(decision.RemoteGame.Release)).ToList();

                    if (matchingReports.Any())
                    {
                        var matchingReport = matchingReports.First();

                        if (matchingReport.Reason != reason)
                        {
                            if (matchingReport.Reason == PendingReleaseReason.DownloadClientUnavailable)
                            {
                                _logger.Debug("The release {0} is already pending with reason {1}, not changing reason", decision.RemoteGame, matchingReport.Reason);
                            }
                            else
                            {
                                _logger.Debug("The release {0} is already pending with reason {1}, changing to {2}", decision.RemoteGame, matchingReport.Reason, reason);
                                matchingReport.Reason = reason;
                                _repository.Update(matchingReport);
                            }
                        }
                        else
                        {
                            _logger.Debug("The release {0} is already pending with reason {1}, not adding again", decision.RemoteGame, reason);
                        }

                        if (matchingReports.Count > 1)
                        {
                            _logger.Debug("The release {0} had {1} duplicate pending, removing duplicates.", decision.RemoteGame, matchingReports.Count - 1);

                            foreach (var duplicate in matchingReports.Skip(1))
                            {
                                _repository.Delete(duplicate.Id);
                                alreadyPending.Remove(duplicate);
                            }
                        }

                        continue;
                    }

                    _logger.Debug("Adding release {0} to pending releases with reason {1}", decision.RemoteGame, reason);
                    Insert(decision, reason);
                }
            }
        }

        public List<ReleaseInfo> GetPending()
        {
            var releases = _repository.All().Select(p =>
            {
                var release = p.Release;

                release.PendingReleaseReason = p.Reason;

                return release;
            }).ToList();

            if (releases.Any())
            {
                releases = FilterBlockedIndexers(releases);
            }

            return releases;
        }

        private List<ReleaseInfo> FilterBlockedIndexers(List<ReleaseInfo> releases)
        {
            var blockedIndexers = new HashSet<int>(_indexerStatusService.GetBlockedProviders().Select(v => v.ProviderId));

            return releases.Where(release => !blockedIndexers.Contains(release.IndexerId)).ToList();
        }

        public List<RemoteGame> GetPendingRemoteGames(int gameId)
        {
            return IncludeRemoteGames(_repository.AllByGameId(gameId)).Select(v => v.RemoteGame).ToList();
        }

        public List<Queue.Queue> GetPendingQueue()
        {
            var queued = new List<Queue.Queue>();

            var nextRssSync = new Lazy<DateTime>(() => _taskManager.GetNextExecution(typeof(RssSyncCommand)));

            var pendingReleases = IncludeRemoteGames(_repository.WithoutFallback());

            foreach (var pendingRelease in pendingReleases)
            {
                if (pendingRelease.RemoteGame.Game == null)
                {
                    var noGameItem = GetQueueItem(pendingRelease, nextRssSync, null);

                    noGameItem.ErrorMessage = "Unable to find matching game(s)";

                    queued.Add(noGameItem);

                    continue;
                }

                queued.Add(GetQueueItem(pendingRelease, nextRssSync, pendingRelease.RemoteGame.Game));
            }

            // Return best quality release for each game
            var deduped = queued.Where(q => q.Game != null).GroupBy(q => q.Game.Id).Select(g =>
            {
                var games = g.First().Game;

                return g.OrderByDescending(e => e.Quality, new QualityModelComparer(games.QualityProfile))
                        .ThenBy(q => PrioritizeDownloadProtocol(q.Game, q.Protocol))
                        .First();
            });

            return deduped.ToList();
        }

        public Queue.Queue FindPendingQueueItem(int queueId)
        {
            return GetPendingQueue().SingleOrDefault(p => p.Id == queueId);
        }

        public void RemovePendingQueueItems(int queueId)
        {
            var targetItem = FindPendingRelease(queueId);
            var gameReleases = _repository.AllByGameId(targetItem.GameId);

            var releasesToRemove = gameReleases.Where(c => c.ParsedGameInfo.PrimaryGameTitle == targetItem.ParsedGameInfo.PrimaryGameTitle);

            _repository.DeleteMany(releasesToRemove.Select(c => c.Id));
        }

        public RemoteGame OldestPendingRelease(int gameId)
        {
            var gameReleases = GetPendingReleases(gameId);

            return gameReleases.Select(r => r.RemoteGame)
                                 .MaxBy(p => p.Release.AgeHours);
        }

        private List<PendingRelease> GetPendingReleases()
        {
            return IncludeRemoteGames(_repository.All().ToList());
        }

        private List<PendingRelease> GetPendingReleases(int gameId)
        {
            return IncludeRemoteGames(_repository.AllByGameId(gameId).ToList());
        }

        private List<PendingRelease> IncludeRemoteGames(List<PendingRelease> releases, Dictionary<string, RemoteGame> knownRemoteGames = null)
        {
            var result = new List<PendingRelease>();

            var gameMap = new Dictionary<int, Game>();

            if (knownRemoteGames != null)
            {
                foreach (var game in knownRemoteGames.Values.Select(v => v.Game))
                {
                    gameMap.TryAdd(game.Id, game);
                }
            }

            foreach (var game in _gameService.GetGames(releases.Select(v => v.GameId).Distinct().Where(v => !gameMap.ContainsKey(v))))
            {
                gameMap[game.Id] = game;
            }

            foreach (var release in releases)
            {
                var game = gameMap.GetValueOrDefault(release.GameId);

                // Just in case the game was removed, but wasn't cleaned up yet (housekeeper will clean it up)
                if (game == null)
                {
                    continue;
                }

                // Languages will be empty if added before upgrading to v4, reparsing the languages if they're empty will set it to Unknown or better.
                if (release.ParsedGameInfo.Languages.Empty())
                {
                    release.ParsedGameInfo.Languages = LanguageParser.ParseLanguages(release.Title);
                }

                release.RemoteGame = new RemoteGame
                {
                    Game = game,
                    GameMatchType = release.AdditionalInfo?.GameMatchType ?? GameMatchType.Unknown,
                    ReleaseSource = release.AdditionalInfo?.ReleaseSource ?? ReleaseSourceType.Unknown,
                    ParsedGameInfo = release.ParsedGameInfo,
                    Release = release.Release
                };

                _aggregationService.Augment(release.RemoteGame);
                release.RemoteGame.CustomFormats = _formatCalculator.ParseCustomFormat(release.RemoteGame, release.Release.Size);

                result.Add(release);
            }

            return result;
        }

        private Queue.Queue GetQueueItem(PendingRelease pendingRelease, Lazy<DateTime> nextRssSync, Game game)
        {
            var ect = pendingRelease.Release.PublishDate.AddMinutes(GetDelay(pendingRelease.RemoteGame));

            if (ect < nextRssSync.Value)
            {
                ect = nextRssSync.Value;
            }
            else
            {
                ect = ect.AddMinutes(_configService.RssSyncInterval);
            }

            var timeLeft = ect.Subtract(DateTime.UtcNow);

            if (timeLeft.TotalSeconds < 0)
            {
                timeLeft = TimeSpan.Zero;
            }

            string downloadClientName = null;
            var indexer = _indexerFactory.Find(pendingRelease.Release.IndexerId);

            if (indexer is { DownloadClientId: > 0 })
            {
                var downloadClient = _downloadClientFactory.Find(indexer.DownloadClientId);

                downloadClientName = downloadClient?.Name;
            }

            var queue = new Queue.Queue
            {
                Id = GetQueueId(pendingRelease, game),
                Game = game,
                Quality = pendingRelease.RemoteGame.ParsedGameInfo?.Quality ?? new QualityModel(),
                Languages = pendingRelease.RemoteGame.Languages,
                Title = pendingRelease.Title,
                Size = pendingRelease.RemoteGame.Release.Size,
                SizeLeft = pendingRelease.RemoteGame.Release.Size,
                RemoteGame = pendingRelease.RemoteGame,
                TimeLeft = timeLeft,
                EstimatedCompletionTime = ect,
                Added = pendingRelease.Added,
                Status = Enum.TryParse(pendingRelease.Reason.ToString(), out QueueStatus outValue) ? outValue : QueueStatus.Unknown,
                Protocol = pendingRelease.RemoteGame.Release.DownloadProtocol,
                Indexer = pendingRelease.RemoteGame.Release.Indexer,
                DownloadClient = downloadClientName
            };

            return queue;
        }

        private void Insert(DownloadDecision decision, PendingReleaseReason reason)
        {
            var release = new PendingRelease
            {
                GameId = decision.RemoteGame.Game.Id,
                ParsedGameInfo = decision.RemoteGame.ParsedGameInfo,
                Release = decision.RemoteGame.Release,
                Title = decision.RemoteGame.Release.Title,
                Added = DateTime.UtcNow,
                Reason = reason,
                AdditionalInfo = new PendingReleaseAdditionalInfo
                {
                    GameMatchType = decision.RemoteGame.GameMatchType,
                    ReleaseSource = decision.RemoteGame.ReleaseSource
                }
            };

            if (release.ParsedGameInfo == null)
            {
                _logger.Warn("Pending release {0} does not have ParsedGameInfo, will cause issues.", release.Title);
            }

            _repository.Insert(release);

            _eventAggregator.PublishEvent(new PendingReleasesUpdatedEvent());
        }

        private void Delete(PendingRelease pendingRelease)
        {
            _repository.Delete(pendingRelease);
            _eventAggregator.PublishEvent(new PendingReleasesUpdatedEvent());
        }

        private static Func<PendingRelease, bool> MatchingReleasePredicate(ReleaseInfo release)
        {
            return p => p.Title == release.Title &&
                   p.Release.PublishDate == release.PublishDate &&
                   p.Release.Indexer == release.Indexer;
        }

        private int GetDelay(RemoteGame remoteGame)
        {
            var delayProfile = _delayProfileService.AllForTags(remoteGame.Game.Tags).OrderBy(d => d.Order).First();
            var delay = delayProfile.GetProtocolDelay(remoteGame.Release.DownloadProtocol);
            var minimumAge = _configService.MinimumAge;

            return new[] { delay, minimumAge }.Max();
        }

        private void RemoveGrabbed(RemoteGame remoteGame)
        {
            var pendingReleases = GetPendingReleases(remoteGame.Game.Id);

            var existingReports = pendingReleases.Where(r => r.RemoteGame.Game.Id == remoteGame.Game.Id)
                                                             .ToList();

            if (existingReports.Empty())
            {
                return;
            }

            var profile = remoteGame.Game.QualityProfile;

            foreach (var existingReport in existingReports)
            {
                var compare = new QualityModelComparer(profile).Compare(remoteGame.ParsedGameInfo.Quality,
                                                                        existingReport.RemoteGame.ParsedGameInfo.Quality);

                // Only remove lower/equal quality pending releases
                // It is safer to retry these releases on the next round than remove it and try to re-add it (if its still in the feed)
                if (compare >= 0)
                {
                    _logger.Debug("Removing previously pending release, as it was grabbed.");
                    Delete(existingReport);
                }
            }
        }

        private void RemoveRejected(List<DownloadDecision> rejected)
        {
            _logger.Debug("Removing failed releases from pending");
            var pending = GetPendingReleases();

            foreach (var rejectedRelease in rejected)
            {
                var matching = pending.Where(MatchingReleasePredicate(rejectedRelease.RemoteGame.Release));

                foreach (var pendingRelease in matching)
                {
                    _logger.Debug("Removing previously pending release, as it has now been rejected.");
                    Delete(pendingRelease);
                }
            }
        }

        private PendingRelease FindPendingRelease(int queueId)
        {
            return GetPendingReleases().First(p => queueId == GetQueueId(p, p.RemoteGame.Game));
        }

        private int GetQueueId(PendingRelease pendingRelease, Game game)
        {
            return HashConverter.GetHashInt31(string.Format("pending-{0}-game{1}", pendingRelease.Id, game?.Id ?? 0));
        }

        private int PrioritizeDownloadProtocol(Game game, DownloadProtocol downloadProtocol)
        {
            var delayProfile = _delayProfileService.BestForTags(game.Tags);

            if (downloadProtocol == delayProfile.PreferredProtocol)
            {
                return 0;
            }

            return 1;
        }

        public void Handle(GamesDeletedEvent message)
        {
            _repository.DeleteByGameIds(message.Games.Select(m => m.Id).ToList());
        }

        public void Handle(GameGrabbedEvent message)
        {
            RemoveGrabbed(message.Game);
        }

        public void Handle(RssSyncCompleteEvent message)
        {
            RemoveRejected(message.ProcessedDecisions.Rejected);
        }
    }
}
