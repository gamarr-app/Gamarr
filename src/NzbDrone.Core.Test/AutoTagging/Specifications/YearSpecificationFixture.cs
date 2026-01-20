using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.AutoTagging.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AutoTagging.Specifications
{
    [TestFixture]
    public class YearSpecificationFixture : CoreTest<YearSpecification>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 1,
                Year = 2020
            };

            Subject.Min = 2015;
            Subject.Max = 2025;
        }

        [Test]
        public void should_return_true_when_year_is_within_range()
        {
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_year_equals_min()
        {
            _game.Year = 2015;
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_year_equals_max()
        {
            _game.Year = 2025;
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_year_is_below_min()
        {
            _game.Year = 2010;
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_year_is_above_max()
        {
            _game.Year = 2030;
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_negate_result_when_negate_is_true()
        {
            Subject.Negate = true;
            _game.Year = 2020;
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_true_for_out_of_range_when_negate_is_true()
        {
            Subject.Negate = true;
            _game.Year = 2010;
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_validate_min_is_required()
        {
            Subject.Min = 0;
            Subject.Max = 2025;

            var result = Subject.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_validate_max_is_greater_or_equal_to_min()
        {
            Subject.Min = 2025;
            Subject.Max = 2015;

            var result = Subject.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_pass_validation_with_valid_range()
        {
            Subject.Min = 2015;
            Subject.Max = 2025;

            var result = Subject.Validate();
            result.IsValid.Should().BeTrue();
        }
    }
}
