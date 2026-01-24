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
                    IndexerFlags = (IndexerFlags)0
                }
            };
        }

        private void GivenIndexerWithRequiredFlags(params int[] flags)
        {
            var settings = new Mock<ITorrentIndexerSettings>();
            settings.SetupGet(s => s.RequiredFlags).Returns(new List<int>(flags));

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.Get(_remoteGame.Release.IndexerId))
                  .Returns(new IndexerDefinition { Settings = settings.Object });
        }

        private void GivenNoIndexer()
        {
            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.Get(_remoteGame.Release.IndexerId))
                  .Returns((IndexerDefinition)null);
        }

        [Test]
        public void should_accept_when_no_indexer_found()
        {
            GivenNoIndexer();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_no_required_flags()
        {
            GivenIndexerWithRequiredFlags();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_release_has_required_flag()
        {
            GivenIndexerWithRequiredFlags((int)IndexerFlags.G_Freeleech);
            _remoteGame.Release.IndexerFlags = IndexerFlags.G_Freeleech;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_release_does_not_have_required_flag()
        {
            GivenIndexerWithRequiredFlags((int)IndexerFlags.G_Freeleech);
            _remoteGame.Release.IndexerFlags = (IndexerFlags)0;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_when_release_has_one_of_multiple_required_flags()
        {
            GivenIndexerWithRequiredFlags((int)IndexerFlags.G_Freeleech, (int)IndexerFlags.G_Halfleech);
            _remoteGame.Release.IndexerFlags = IndexerFlags.G_Halfleech;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_release_has_none_of_multiple_required_flags()
        {
            GivenIndexerWithRequiredFlags((int)IndexerFlags.G_Freeleech, (int)IndexerFlags.G_Halfleech);
            _remoteGame.Release.IndexerFlags = IndexerFlags.G_Internal;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_have_default_priority()
        {
            Subject.Priority.Should().Be(SpecificationPriority.Default);
        }

        [Test]
        public void should_have_permanent_rejection_type()
        {
            Subject.Type.Should().Be(RejectionType.Permanent);
        }
    }
}
