using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Games;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class FailedGrabServiceFixture : CoreTest<FailedGrabService>
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            var release = Builder<TorrentInfo>.CreateNew()
                .With(v => v.Guid = "http://indexer/release-1")
                .With(v => v.Title = "A Game 2026")
                .With(v => v.Indexer = "Knaben")
                .With(v => v.IndexerId = 5)
                .With(v => v.DownloadProtocol = DownloadProtocol.Torrent)
                .With(v => v.DownloadUrl = "http://indexer/download/1")
                .With(v => v.MagnetUrl = null)
                .With(v => v.InfoHash = null)
                .Build();

            _remoteGame = new RemoteGame
            {
                Game = Builder<Game>.CreateNew().With(g => g.Id = 43).Build(),
                Release = release,
                ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel() }
            };

            WithGrabFailures(0);
        }

        private void WithGrabFailures(int count, string guid = "http://indexer/release-1")
        {
            var existing = Enumerable.Range(0, count)
                                     .Select(_ =>
                                     {
                                         var history = new GameHistory { EventType = GameHistoryEventType.GrabFailed, GameId = 43 };
                                         history.Data["Guid"] = guid;

                                         return history;
                                     })
                                     .ToList();

            Mocker.GetMock<IHistoryService>()
                  .Setup(v => v.GetByGameId(43, GameHistoryEventType.GrabFailed))
                  .Returns(existing);
        }

        private void VerifyBlocklisted(Times times)
        {
            Mocker.GetMock<IBlocklistService>()
                  .Verify(v => v.Block(It.IsAny<RemoteGame>(), It.IsAny<string>()), times);
        }

        [Test]
        public void should_not_blocklist_on_the_first_failure()
        {
            WithGrabFailures(1);

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            VerifyBlocklisted(Times.Never());
        }

        [Test]
        public void should_not_blocklist_below_the_attempt_cap()
        {
            WithGrabFailures(FailedGrabService.MaximumGrabAttempts - 1);

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            VerifyBlocklisted(Times.Never());
        }

        [Test]
        public void should_blocklist_once_the_attempt_cap_is_reached()
        {
            WithGrabFailures(FailedGrabService.MaximumGrabAttempts);

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            // Blocklisting rather than a private cooldown is the point: BlocklistSpecification
            // rejects permanently, so the decision engine stops selecting the release at all.
            Mocker.GetMock<IBlocklistService>()
                  .Verify(v => v.Block(_remoteGame, It.Is<string>(m => m.Contains("429 TooManyRequests"))), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_only_count_failures_of_the_same_release()
        {
            // Plenty of failures for the game, none of them this release.
            WithGrabFailures(FailedGrabService.MaximumGrabAttempts + 5, "http://indexer/some-other-release");

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            VerifyBlocklisted(Times.Never());
        }

        [Test]
        public void should_not_blocklist_a_release_that_is_already_blocklisted()
        {
            WithGrabFailures(FailedGrabService.MaximumGrabAttempts + 1);

            Mocker.GetMock<IBlocklistService>()
                  .Setup(v => v.Blocklisted(43, It.IsAny<ReleaseInfo>()))
                  .Returns(true);

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            VerifyBlocklisted(Times.Never());
        }

        [Test]
        public void should_do_nothing_when_the_release_never_matched_a_game()
        {
            _remoteGame.Game = null;

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            VerifyBlocklisted(Times.Never());

            Mocker.GetMock<IHistoryService>()
                  .Verify(v => v.GetByGameId(It.IsAny<int>(), It.IsAny<GameHistoryEventType?>()), Times.Never());
        }

        [Test]
        public void should_do_nothing_when_the_release_has_no_guid()
        {
            _remoteGame.Release.Guid = null;

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            VerifyBlocklisted(Times.Never());
        }

        [Test]
        public void should_count_failures_from_rows_stored_with_a_camel_cased_guid_key()
        {
            // HistoryService writes "Guid", but the Data column is serialised camelCased, so rows
            // read back from the database carry "guid" in a dictionary that need not have the
            // case-insensitive comparer the GameHistory constructor sets up. An exact-case lookup
            // counts zero failures forever and the cap never fires - which is what the live
            // instance actually did until this was pinned.
            var histories = Enumerable.Range(0, FailedGrabService.MaximumGrabAttempts)
                                      .Select(_ => new GameHistory
                                      {
                                          EventType = GameHistoryEventType.GrabFailed,
                                          GameId = 43,
                                          Data = new Dictionary<string, string>(StringComparer.Ordinal)
                                          {
                                              { "guid", "http://indexer/release-1" }
                                          }
                                      })
                                      .ToList();

            Mocker.GetMock<IHistoryService>()
                  .Setup(v => v.GetByGameId(43, GameHistoryEventType.GrabFailed))
                  .Returns(histories);

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            VerifyBlocklisted(Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_tolerate_history_rows_with_no_guid_recorded()
        {
            var histories = new List<GameHistory>
            {
                new GameHistory { EventType = GameHistoryEventType.GrabFailed, GameId = 43 }
            };

            Mocker.GetMock<IHistoryService>()
                  .Setup(v => v.GetByGameId(43, GameHistoryEventType.GrabFailed))
                  .Returns(histories);

            Subject.HandleAsync(new GameGrabFailedEvent(_remoteGame, "429 TooManyRequests"));

            VerifyBlocklisted(Times.Never());
        }
    }
}
