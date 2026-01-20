using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class RequiredIndexerFlagsSpecificationFixture : CoreTest<RequiredIndexerFlagsSpecification>
    {
        private RemoteGame _remoteGame;
        private IndexerDefinition _indexerDefinition;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                Game = new Game { Id = 1 },
                Release = new ReleaseInfo
                {
                    Title = "Game.Title.2023",
                    IndexerId = 1,
                    DownloadProtocol = DownloadProtocol.Torrent,
                    IndexerFlags = IndexerFlags.None
                }
            };

            _indexerDefinition = new IndexerDefinition
            {
                Id = 1,
                Settings = new TorrentRssIndexerSettings()
            };

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.Get(It.IsAny<int>()))
                  .Returns(_indexerDefinition);
        }

        private void GivenRequiredFlags(params IndexerFlags[] flags)
        {
            var requiredFlags = new List<int>();
            foreach (var flag in flags)
            {
                requiredFlags.Add((int)flag);
            }

            var settings = new TorrentRssIndexerSettings { RequiredFlags = requiredFlags };
            _indexerDefinition.Settings = settings;
        }

        private void GivenReleaseFlags(IndexerFlags flags)
        {
            _remoteGame.Release.IndexerFlags = flags;
        }

        private void GivenIndexerNotFound()
        {
            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.Get(It.IsAny<int>()))
                  .Throws(new Exception("Indexer not found"));
        }

        private void GivenNullIndexer()
        {
            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.Get(It.IsAny<int>()))
                  .Returns((IndexerDefinition)null);
        }

        [Test]
        public void should_return_true_when_no_required_flags()
        {
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_indexer_not_found()
        {
            GivenIndexerNotFound();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_indexer_is_null()
        {
            GivenNullIndexer();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_required_flag_is_present()
        {
            GivenRequiredFlags(IndexerFlags.Freeleech);
            GivenReleaseFlags(IndexerFlags.Freeleech);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_one_of_required_flags_is_present()
        {
            GivenRequiredFlags(IndexerFlags.Freeleech, IndexerFlags.HalfLeech);
            GivenReleaseFlags(IndexerFlags.Freeleech);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_required_flag_is_missing()
        {
            GivenRequiredFlags(IndexerFlags.Freeleech);
            GivenReleaseFlags(IndexerFlags.None);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_none_of_required_flags_are_present()
        {
            GivenRequiredFlags(IndexerFlags.Freeleech, IndexerFlags.HalfLeech);
            GivenReleaseFlags(IndexerFlags.Internal);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_have_permanent_rejection_type()
        {
            Subject.Type.Should().Be(RejectionType.Permanent);
        }

        [Test]
        public void should_have_default_priority()
        {
            Subject.Priority.Should().Be(SpecificationPriority.Default);
        }

        [Test]
        public void should_return_rejection_reason_when_flags_missing()
        {
            GivenRequiredFlags(IndexerFlags.Freeleech);
            GivenReleaseFlags(IndexerFlags.None);

            var result = Subject.IsSatisfiedBy(_remoteGame, null);
            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.RequiredFlags);
        }
    }
}
