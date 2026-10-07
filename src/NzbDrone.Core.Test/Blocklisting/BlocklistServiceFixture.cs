using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
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
        public void should_return_true_when_same_infohash_is_offered_by_a_different_indexer()
        {
            // The same torrent re-offered under a second indexer label. The stored row's
            // indexer differs, so before the hash hit short-circuited this it fell through
            // to title+indexer matching and the re-push was grabbed again.
            _blocklist.Indexer = "BigFANGroup (Prowlarr)";
            _torrentInfo.Indexer = "1337x (Prowlarr)";

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTorrentInfoHash(1, _torrentInfo.InfoHash))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_title_matches_but_infohash_differs()
        {
            // Over-block: a blocklisted release must not block a genuinely different
            // torrent that happens to share the title and indexer.
            _blocklist.TorrentInfoHash = "0000000000000000000000000000000000000000";
            _torrentInfo.InfoHash = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeFalse();
        }

        [Test]
        public void should_still_block_legacy_row_with_no_stored_infohash()
        {
            // Every row written before the hash was recorded has a null TorrentInfoHash.
            // Those must keep blocking on title+indexer even when the incoming release
            // does carry a hash, or an upgrade silently empties the blocklist.
            _blocklist.TorrentInfoHash = null;

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [Test]
        public void should_block_hashless_release_from_another_indexer_when_size_matches()
        {
            // The decision-engine moment for a pushed release: the infohash isn't known
            // until the .torrent is fetched, so the only evidence is the exact title, the
            // game and the size.
            _torrentInfo.InfoHash = null;
            _blocklist.Indexer = "BigFANGroup (Prowlarr)";
            _torrentInfo.Indexer = "1337x (Prowlarr)";

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [Test]
        public void should_not_block_hashless_release_from_another_indexer_when_size_differs()
        {
            _torrentInfo.InfoHash = null;
            _torrentInfo.Size = _blocklist.Size.Value + 500.Megabytes();
            _blocklist.Indexer = "BigFANGroup (Prowlarr)";
            _torrentInfo.Indexer = "1337x (Prowlarr)";

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeFalse();
        }

        [Test]
        public void should_not_block_hashless_push_from_same_indexer_when_size_differs()
        {
            // The reported bug. A Prowlarr push carries no infohash -- nothing parses a
            // magnet at push time and the hash is only learned in TorrentClientBase at
            // grab time, strictly after this spec runs -- so an exact title match plus an
            // equal indexer label was enough to reject a genuinely different torrent of
            // the same game. The sizes differ by gigabytes; these are not one release.
            _torrentInfo.InfoHash = null;
            _blocklist.TorrentInfoHash = null;
            _blocklist.Size = 3.Gigabytes();
            _torrentInfo.Size = 7.Gigabytes();

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeFalse();
        }

        [Test]
        public void should_block_hashless_push_from_same_indexer_when_size_matches()
        {
            _torrentInfo.InfoHash = null;
            _blocklist.TorrentInfoHash = null;
            _blocklist.Size = 7.Gigabytes();
            _torrentInfo.Size = 7.Gigabytes();

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [TestCase(null)]
        [TestCase(0L)]
        public void should_still_block_row_with_no_usable_size_from_same_indexer(long? storedSize)
        {
            // Legacy rows, and rows written from a history payload that recorded no size
            // (parsed as 0), must keep blocking on title + indexer exactly as before --
            // adding the size requirement must not un-blocklist anything that works today.
            _torrentInfo.InfoHash = null;
            _blocklist.TorrentInfoHash = null;
            _blocklist.Size = storedSize;
            _torrentInfo.Size = 7.Gigabytes();

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [Test]
        public void should_still_block_when_incoming_release_has_no_usable_size()
        {
            _torrentInfo.InfoHash = null;
            _blocklist.TorrentInfoHash = null;
            _blocklist.Size = 7.Gigabytes();
            _torrentInfo.Size = 0;

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [Test]
        public void should_still_block_blank_indexer_row_when_size_matches()
        {
            _torrentInfo.InfoHash = null;
            _blocklist.TorrentInfoHash = null;
            _blocklist.Indexer = null;
            _blocklist.Size = 7.Gigabytes();
            _torrentInfo.Size = 7.Gigabytes();

            Mocker.GetMock<IBlocklistRepository>()
                  .Setup(s => s.BlocklistedByTitle(1, _torrentInfo.Title))
                  .Returns(new List<Blocklist> { _blocklist });

            Subject.Blocklisted(1, _torrentInfo).Should().BeTrue();
        }

        [Test]
        public void should_persist_torrent_info_hash_from_tracked_download()
        {
            _event.Data["protocol"] = ((int)DownloadProtocol.Torrent).ToString();
            _event.DownloadId = "511567EACF51EE0B303D2A9B9EDB4A9B214B3D92";
            _event.TrackedDownload = new TrackedDownload
            {
                Protocol = DownloadProtocol.Torrent,
                DownloadItem = new DownloadClientItem { DownloadId = _event.DownloadId }
            };

            Subject.Handle(_event);

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(v => v.Insert(It.Is<Blocklist>(b =>
                      b.TorrentInfoHash == "511567EACF51EE0B303D2A9B9EDB4A9B214B3D92")), Times.Once());
        }

        [Test]
        public void should_persist_download_id_as_torrent_info_hash_when_there_is_no_tracked_download()
        {
            // Manual "mark as failed" publishes no TrackedDownload, and the grab only
            // recorded a torrentInfoHash when the indexer supplied one up front. Without
            // this fallback the row is written with a null hash and can never be matched
            // by hash afterwards.
            _event.Data["protocol"] = ((int)DownloadProtocol.Torrent).ToString();
            _event.DownloadId = "511567EACF51EE0B303D2A9B9EDB4A9B214B3D92";

            Subject.Handle(_event);

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(v => v.Insert(It.Is<Blocklist>(b =>
                      b.TorrentInfoHash == "511567EACF51EE0B303D2A9B9EDB4A9B214B3D92")), Times.Once());
        }

        [Test]
        public void should_prefer_recorded_torrent_info_hash_over_download_id()
        {
            _event.Data["protocol"] = ((int)DownloadProtocol.Torrent).ToString();
            _event.Data["torrentInfoHash"] = "AAAABBBBCCCCDDDDEEEEFFFF00001111222233334";
            _event.DownloadId = "511567EACF51EE0B303D2A9B9EDB4A9B214B3D92";

            Subject.Handle(_event);

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(v => v.Insert(It.Is<Blocklist>(b =>
                      b.TorrentInfoHash == "AAAABBBBCCCCDDDDEEEEFFFF00001111222233334")), Times.Once());
        }

        [Test]
        public void should_not_persist_download_id_as_torrent_info_hash_for_usenet()
        {
            // _event is usenet (protocol 1); a Sabnzbd id is not an infohash.
            Subject.Handle(_event);

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(v => v.Insert(It.Is<Blocklist>(b => b.TorrentInfoHash == null)), Times.Once());
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
                  .Verify(s => s.Purge(false), Times.Once());
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
                  .Verify(s => s.DeleteForGames(It.IsAny<List<int>>()), Times.Once());
        }
    }
}
