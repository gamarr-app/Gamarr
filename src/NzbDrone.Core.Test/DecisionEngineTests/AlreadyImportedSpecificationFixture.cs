using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class AlreadyImportedSpecificationFixture : CoreTest<AlreadyImportedSpecification>
    {
        private const int FIRST_GAME_ID = 1;
        private const string TITLE = "Game.Title.2018.720p.HDTV.x264-Gamarr";

        private Game _game;
        private QualityModel _hdtv720p;
        private QualityModel _hdtv1080p;
        private RemoteGame _remoteGame;
        private List<GameHistory> _history;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                    .With(m => m.Id = FIRST_GAME_ID)
                                    .With(m => m.GameFileId = 1)
                                    .Build();

            _hdtv720p = new QualityModel(Quality.Uplay, new Revision(version: 1));
            _hdtv1080p = new QualityModel(Quality.Origin, new Revision(version: 1));

            _remoteGame = new RemoteGame
            {
                Game = _game,
                ParsedGameInfo = new ParsedGameInfo { Quality = _hdtv720p },
                Release = Builder<ReleaseInfo>.CreateNew()
                                              .Build()
            };

            _history = new List<GameHistory>();

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.EnableCompletedDownloadHandling)
                  .Returns(true);

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.GetByGameId(It.IsAny<int>(), null))
                  .Returns(_history);
        }

        private void GivenCdhDisabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.EnableCompletedDownloadHandling)
                  .Returns(false);
        }

        private void GivenHistoryItem(string downloadId, string sourceTitle, QualityModel quality, GameHistoryEventType eventType)
        {
            _history.Add(new GameHistory
                         {
                             DownloadId = downloadId,
                             SourceTitle = sourceTitle,
                             Quality = quality,
                             Date = DateTime.UtcNow,
                             EventType = eventType
                         });
        }

        [Test]
        public void should_be_accepted_if_CDH_is_disabled()
        {
            GivenCdhDisabled();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_game_does_not_have_a_file()
        {
            _remoteGame.Game.GameFileId = 0;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_game_does_not_have_grabbed_event()
        {
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_game_does_not_have_imported_event()
        {
            GivenHistoryItem(Guid.NewGuid().ToString().ToUpper(), TITLE, _hdtv720p, GameHistoryEventType.Grabbed);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_grabbed_and_imported_quality_is_the_same()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(downloadId, TITLE, _hdtv720p, GameHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _hdtv720p, GameHistoryEventType.DownloadFolderImported);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_grabbed_download_id_and_release_torrent_hash_is_unknown()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(downloadId, TITLE, _hdtv720p, GameHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _hdtv1080p, GameHistoryEventType.DownloadFolderImported);

            _remoteGame.Release = Builder<TorrentInfo>.CreateNew()
                                                         .With(t => t.DownloadProtocol = DownloadProtocol.Torrent)
                                                         .With(t => t.InfoHash = null)
                                                         .Build();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_grabbed_download_does_not_have_an_id()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(null, TITLE, _hdtv720p, GameHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _hdtv1080p, GameHistoryEventType.DownloadFolderImported);

            _remoteGame.Release = Builder<TorrentInfo>.CreateNew()
                                                         .With(t => t.DownloadProtocol = DownloadProtocol.Torrent)
                                                         .With(t => t.InfoHash = downloadId)
                                                         .Build();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_rejected_if_grabbed_download_id_matches_release_torrent_hash()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(downloadId, TITLE, _hdtv720p, GameHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _hdtv1080p, GameHistoryEventType.DownloadFolderImported);

            _remoteGame.Release = Builder<TorrentInfo>.CreateNew()
                                                         .With(t => t.DownloadProtocol = DownloadProtocol.Torrent)
                                                         .With(t => t.InfoHash = downloadId)
                                                         .Build();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_rejected_if_release_title_matches_grabbed_event_source_title()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(downloadId, TITLE, _hdtv720p, GameHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _hdtv1080p, GameHistoryEventType.DownloadFolderImported);

            _remoteGame.Release = Builder<TorrentInfo>.CreateNew()
                                                         .With(t => t.DownloadProtocol = DownloadProtocol.Torrent)
                                                         .With(t => t.InfoHash = downloadId)
                                                         .Build();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }
    }
}
