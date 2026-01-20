using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class BlocklistSpecificationFixture : CoreTest<BlocklistSpecification>
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
                    DownloadProtocol = DownloadProtocol.Torrent
                }
            };

            Mocker.GetMock<IBlocklistService>()
                  .Setup(s => s.Blocklisted(It.IsAny<int>(), It.IsAny<ReleaseInfo>()))
                  .Returns(false);
        }

        private void GivenBlocklistedRelease()
        {
            Mocker.GetMock<IBlocklistService>()
                  .Setup(s => s.Blocklisted(_remoteGame.Game.Id, _remoteGame.Release))
                  .Returns(true);
        }

        [Test]
        public void should_return_true_if_release_is_not_blocklisted()
        {
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_release_is_blocklisted()
        {
            GivenBlocklistedRelease();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_have_permanent_rejection_type()
        {
            Subject.Type.Should().Be(RejectionType.Permanent);
        }

        [Test]
        public void should_have_database_priority()
        {
            Subject.Priority.Should().Be(SpecificationPriority.Database);
        }

        [Test]
        public void should_check_blocklist_with_correct_game_id()
        {
            Subject.IsSatisfiedBy(_remoteGame, null);

            Mocker.GetMock<IBlocklistService>()
                  .Verify(s => s.Blocklisted(_remoteGame.Game.Id, _remoteGame.Release), Times.Once());
        }

        [Test]
        public void should_return_rejection_reason_when_blocklisted()
        {
            GivenBlocklistedRelease();

            var result = Subject.IsSatisfiedBy(_remoteGame, null);
            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.Blocklisted);
        }
    }
}
