using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class AcceptableSizeSpecificationFixture : CoreTest<AcceptableSizeSpecification>
    {
        private Game _game;
        private RemoteGame _remoteGame;
        private QualityDefinition _qualityType;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew().Build();

            _qualityType = Builder<QualityDefinition>.CreateNew()
                .With(q => q.MinSize = 2)
                .With(q => q.MaxSize = 10)
                .With(q => q.Quality = Quality.Scene)
                .Build();

            _remoteGame = new RemoteGame
            {
                Game = _game,
                Release = new ReleaseInfo(),
                ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.Scene, new Revision(version: 2)) },
            };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<IQualityDefinitionService>().Setup(s => s.Get(Quality.Scene)).Returns(_qualityType);
        }

        [TestCase(30, 50, true)]
        [TestCase(30, 250, true)]
        [TestCase(30, 500, true)]
        [TestCase(60, 100, true)]
        [TestCase(60, 500, true)]
        [TestCase(60, 1000, true)]
        public void single_episode(int runtime, int sizeInMegaBytes, bool expectedResult)
        {
            _game.GameMetadata.Value.Runtime = runtime;
            _remoteGame.Game = _game;
            _remoteGame.Release.Size = sizeInMegaBytes.Megabytes();

            // For games, size checks are always accepted regardless of runtime
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().Be(expectedResult);
        }

        [Test]
        public void should_return_true_if_size_is_zero()
        {
            _game.GameMetadata.Value.Runtime = 120;
            _remoteGame.Game = _game;
            _remoteGame.Release.Size = 0;
            _qualityType.MinSize = 10;
            _qualityType.MaxSize = 20;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_unlimited_30_minute()
        {
            _game.GameMetadata.Value.Runtime = 30;
            _remoteGame.Game = _game;
            _remoteGame.Release.Size = 18457280000;
            _qualityType.MaxSize = null;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_unlimited_60_minute()
        {
            _game.GameMetadata.Value.Runtime = 60;
            _remoteGame.Game = _game;
            _remoteGame.Release.Size = 36857280000;
            _qualityType.MaxSize = null;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_use_110_minutes_if_runtime_is_0()
        {
            // For games, size checks are always accepted regardless of runtime
            _game.GameMetadata.Value.Runtime = 0;
            _remoteGame.Game = _game;
            _remoteGame.Release.Size = 1095.Megabytes();

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().Be(true);
            _remoteGame.Release.Size = 1105.Megabytes();
            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().Be(true);
        }
    }
}
