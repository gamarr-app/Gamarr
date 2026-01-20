using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class TorrentInfoFixture
    {
        [Test]
        public void should_set_magnet_url()
        {
            var torrent = new TorrentInfo
            {
                MagnetUrl = "magnet:?xt=urn:btih:test"
            };

            torrent.MagnetUrl.Should().Be("magnet:?xt=urn:btih:test");
        }

        [Test]
        public void should_set_info_hash()
        {
            var torrent = new TorrentInfo
            {
                InfoHash = "abc123def456"
            };

            torrent.InfoHash.Should().Be("abc123def456");
        }

        [Test]
        public void should_set_seeders()
        {
            var torrent = new TorrentInfo
            {
                Seeders = 100
            };

            torrent.Seeders.Should().Be(100);
        }

        [Test]
        public void should_set_peers()
        {
            var torrent = new TorrentInfo
            {
                Peers = 50
            };

            torrent.Peers.Should().Be(50);
        }

        [Test]
        public void GetSeeders_should_return_null_for_non_torrent_release()
        {
            var release = new ReleaseInfo();

            TorrentInfo.GetSeeders(release).Should().BeNull();
        }

        [Test]
        public void GetSeeders_should_return_seeders_for_torrent_release()
        {
            var torrent = new TorrentInfo { Seeders = 50 };

            TorrentInfo.GetSeeders(torrent).Should().Be(50);
        }

        [Test]
        public void GetPeers_should_return_null_for_non_torrent_release()
        {
            var release = new ReleaseInfo();

            TorrentInfo.GetPeers(release).Should().BeNull();
        }

        [Test]
        public void GetPeers_should_return_peers_for_torrent_release()
        {
            var torrent = new TorrentInfo { Peers = 25 };

            TorrentInfo.GetPeers(torrent).Should().Be(25);
        }

        [Test]
        public void ToString_with_L_format_should_include_torrent_info()
        {
            var torrent = new TorrentInfo
            {
                Title = "Test",
                MagnetUrl = "magnet:test",
                InfoHash = "abc123",
                Seeders = 10,
                Peers = 20
            };

            var result = torrent.ToString("L");

            result.Should().Contain("MagnetUrl:");
            result.Should().Contain("InfoHash:");
            result.Should().Contain("Seeders:");
            result.Should().Contain("Peers:");
        }

        [Test]
        public void should_inherit_from_release_info()
        {
            var torrent = new TorrentInfo
            {
                Title = "Test Torrent",
                Size = 1024
            };

            torrent.Title.Should().Be("Test Torrent");
            torrent.Size.Should().Be(1024);
        }
    }
}
