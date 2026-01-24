using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class AcceptableSizeSpecificationFixture : CoreTest<AcceptableSizeSpecification>
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                Release = new ReleaseInfo
                {
                    Title = "Game.Title.2023",
                    Size = 50000000000
                },
                ParsedGameInfo = new ParsedGameInfo()
            };
        }

        [Test]
        public void should_accept_any_size()
        {
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_parsed_info_null()
        {
            _remoteGame.ParsedGameInfo = null;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }
    }
}
