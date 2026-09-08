using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class BlockedIndexerSpecificationFixture : CoreTest<BlockedIndexerSpecification>
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                Release = new ReleaseInfo { IndexerId = 1 }
            };

            Mocker.GetMock<IIndexerStatusService>()
                  .Setup(v => v.GetBlockedProviders())
                  .Returns(new List<IndexerStatus>());
        }

        private void WithBlockedIndexer()
        {
            Mocker.GetMock<IIndexerStatusService>()
                  .Setup(v => v.GetBlockedProviders())
                  .Returns(new List<IndexerStatus> { new IndexerStatus { ProviderId = 1, DisabledTill = DateTime.UtcNow } });
        }

        [Test]
        public void should_return_true_if_no_blocked_indexer()
        {
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_blocked_indexer()
        {
            WithBlockedIndexer();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
            Subject.Type.Should().Be(RejectionType.Temporary);
        }

        // A pushed magnet is the recovery route for an indexer Gamarr cannot download from, so
        // it has to survive that indexer being blocked — otherwise it fails only when needed.
        [Test]
        public void should_return_true_for_a_magnet_only_release_from_a_blocked_indexer()
        {
            WithBlockedIndexer();

            _remoteGame.Release = new TorrentInfo { IndexerId = 1, MagnetUrl = "magnet:?xt=urn:btih:abc" };

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        // With a download url present the grab does go back to the indexer, so being blocked
        // still matters even though a magnet is also on offer.
        [Test]
        public void should_return_false_for_a_release_that_also_has_a_download_url()
        {
            WithBlockedIndexer();

            _remoteGame.Release = new TorrentInfo
            {
                IndexerId = 1,
                MagnetUrl = "magnet:?xt=urn:btih:abc",
                DownloadUrl = "http://my.indexer/file.torrent"
            };

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }
    }
}
