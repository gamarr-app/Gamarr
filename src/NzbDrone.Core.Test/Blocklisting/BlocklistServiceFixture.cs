using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Download;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Blocklisting
{
    [TestFixture]
    public class BlocklistServiceFixture : CoreTest<BlocklistService>
    {
        private DownloadFailedEvent _event;
        private ReleaseInfo _releaseInfo;
        private TorrentInfo _torrentInfo;
        private RemoteGame _remoteGame;
        private Blocklist _blocklist;

        [SetUp]
        public void Setup()
        {
            _event = new DownloadFailedEvent
            {
                GameId = 69,
                Quality = new QualityModel(),
                SourceTitle = "series.title.s01e01",
                DownloadClient = "SabnzbdClient",
                DownloadId = "Sabnzbd_nzo_2dfh73k"
            };

            _event.Data.Add("publishedDate", DateTime.UtcNow.ToString("s") + "Z");
            _event.Data.Add("size", "1000");
            _event.Data.Add("indexer", "nzbs.org");
            _event.Data.Add("protocol", "1");
            _event.Data.Add("message", "Marked as failed");

            _releaseInfo = new ReleaseInfo
            {
                Title = "Test Game 2023",
                Indexer = "TestIndexer",
                DownloadProtocol = DownloadProtocol.Usenet,
                PublishDate = DateTime.UtcNow,
                Size = 1000000000
            };

            _torrentInfo = new TorrentInfo
            {
                Title = "Test Game 2023",
                Indexer = "TestIndexer",
                DownloadProtocol = DownloadProtocol.Torrent,
                PublishDate = DateTime.UtcNow,
                Size = 1000000000,
                InfoHash = "ABC123DEF456"
            };

            _remoteGame = new RemoteGame
            {
                Game = new Game { Id = 1, Title = "Test Game" },
                Release = _torrentInfo,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.GOG),
                    Languages = new List<Language> { Language.English }
                }
            };

            _blocklist = new Blocklist
            {
                Id = 1,
                GameId = 1,
                SourceTitle = "Test Game 2023",
                Protocol = DownloadProtocol.Torrent,
                TorrentInfoHash = "ABC123DEF456",
                Indexer = "TestIndexer",
                PublishedDate = DateTime.UtcNow,
                Size = 1000000000
            };

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTorrentInfoHash(It.IsAny<int>(), It.IsAny<string>()))
                  .Returns(new List<Blocklist>());

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(It.IsAny<int>(), It.IsAny<string>()))
                  .Returns(new List<Blocklist>());
        }

        [Test]
        public void should_add_to_repository()
        {
            Subject.Handle(_event);

            Mocker.GetMock<IBlocklistRepository>()
                .Verify(v => v.Insert(It.Is<Blocklist>(b => b.GameId == _event.GameId)), Times.Once());
        }

        [Test]
        public void should_add_to_repository_missing_size_and_protocol()
        {
            Subject.Handle(_event);

            _event.Data.Remove("size");
            _event.Data.Remove("protocol");

            Mocker.GetMock<IBlocklistRepository>()
                .Verify(v => v.Insert(It.Is<Blocklist>(b => b.GameId == _event.GameId)), Times.Once());
        }

        [Test]
        public void should_return_false_when_torrent_not_blocklisted()
        {
            Subject.Blocklisted(1, _torrentInfo).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_torrent_is_blocklisted_by_infohash()
        {
            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTorrentInfoHash(1, _torrentInfo.InfoHash))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_torrent_is_blocklisted_by_title()
        {
            _torrentInfo.InfoHash = null;
            _blocklist.TorrentInfoHash = null;

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_usenet_release_not_blocklisted()
        {
            Subject.Blocklisted(1, _releaseInfo).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_usenet_release_is_blocklisted_by_publish_date()
        {
            _blocklist.Protocol = DownloadProtocol.Usenet;
            _blocklist.PublishedDate = _releaseInfo.PublishDate;

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _releaseInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _releaseInfo).Should().BeTrue();
        }

        [Test]
        public void should_check_torrent_hash_blocklist()
        {
            Subject.BlocklistedTorrentHash(1, "ABC123DEF456").Should().BeFalse();

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(s => s.BlocklistedByTorrentInfoHash(1, "ABC123DEF456"), Times.Once());
        }

        [Test]
        public void should_return_true_when_torrent_hash_is_blocklisted()
        {
            _blocklist.TorrentInfoHash = "ABC123DEF456";

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTorrentInfoHash(1, "ABC123DEF456"))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.BlocklistedTorrentHash(1, "ABC123DEF456").Should().BeTrue();
        }

        [Test]
        public void should_add_blocklist_entry_for_remote_game()
        {
            Subject.Block(_remoteGame, "Test message");

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(s => s.Insert(It.Is<Blocklist>(b =>
                      b.GameId == 1 &&
                      b.SourceTitle == _torrentInfo.Title &&
                      b.TorrentInfoHash == _torrentInfo.InfoHash &&
                      b.Message == "Test message")), Times.Once());
        }

        [Test]
        public void should_delete_blocklist_entry_by_id()
        {
            Subject.Delete(1);

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(s => s.Delete(1), Times.Once());
        }

        [Test]
        public void should_delete_multiple_blocklist_entries()
        {
            var ids = new List<int> { 1, 2, 3 };

            Subject.Delete(ids);

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(s => s.DeleteMany(ids), Times.Once());
        }

        [Test]
        public void should_get_blocklist_by_game_id()
        {
            var expected = new List<Blocklist> { _blocklist };

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByGame(1))
                  .Returns(expected);

            Subject.GetByGameId(1).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void should_purge_blocklist_on_clear_command()
        {
            Subject.Execute(new ClearBlocklistCommand());

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(s => s.Purge(), Times.Once());
        }

        [Test]
        public void should_delete_blocklist_for_deleted_games()
        {
            var games = new List<Game>
            {
                new Game { Id = 1 },
                new Game { Id = 2 }
            };

            var deleteEvent = new GamesDeletedEvent(games, false, false);

            Subject.HandleAsync(deleteEvent);

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(s => s.DeleteForGames(It.Is<List<int>>(ids => ids.Count == 2 && ids.Contains(1) && ids.Contains(2))), Times.Once());
        }
    }
}
