using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.AutoTagging.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AutoTagging.Specifications
{
    [TestFixture]
    public class MonitoredSpecificationFixture : CoreTest<MonitoredSpecification>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 1,
                Monitored = true
            };
        }

        [Test]
        public void should_return_true_when_game_is_monitored()
        {
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_game_is_not_monitored()
        {
            _game.Monitored = false;
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_negate_result_when_negate_is_true()
        {
            Subject.Negate = true;
            _game.Monitored = true;
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_true_for_unmonitored_when_negate_is_true()
        {
            Subject.Negate = true;
            _game.Monitored = false;
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_have_correct_implementation_name()
        {
            Subject.ImplementationName.Should().Be("Monitored");
        }

        [Test]
        public void should_always_pass_validation()
        {
            var result = Subject.Validate();
            result.IsValid.Should().BeTrue();
        }
    }
}
