using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats.Specifications
{
    [TestFixture]
    public class SizeSpecificationFixture : CoreTest<SizeSpecification>
    {
        private CustomFormatInput _input;

        [SetUp]
        public void Setup()
        {
            _input = new CustomFormatInput
            {
                Size = 5L * 1024 * 1024 * 1024 // 5 GB
            };

            Subject.Min = 1; // 1 GB
            Subject.Max = 10; // 10 GB
        }

        [Test]
        public void should_return_true_when_size_is_within_range()
        {
            Subject.IsSatisfiedBy(_input).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_size_is_below_min()
        {
            _input.Size = 500L * 1024 * 1024; // 500 MB

            Subject.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_size_is_above_max()
        {
            _input.Size = 15L * 1024 * 1024 * 1024; // 15 GB

            Subject.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_size_equals_min()
        {
            _input.Size = 1L * 1024 * 1024 * 1024; // 1 GB (min is exclusive)

            Subject.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_size_equals_max()
        {
            _input.Size = 10L * 1024 * 1024 * 1024; // 10 GB (max is inclusive)

            Subject.IsSatisfiedBy(_input).Should().BeTrue();
        }

        [Test]
        public void should_negate_result_when_negate_is_true()
        {
            Subject.Negate = true;

            Subject.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_return_true_for_out_of_range_when_negate_is_true()
        {
            Subject.Negate = true;
            _input.Size = 500L * 1024 * 1024; // 500 MB

            Subject.IsSatisfiedBy(_input).Should().BeTrue();
        }

        [Test]
        public void should_fail_validation_when_min_is_negative()
        {
            Subject.Min = -1;
            Subject.Max = 10;

            var result = Subject.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_fail_validation_when_max_is_less_than_min()
        {
            Subject.Min = 10;
            Subject.Max = 5;

            var result = Subject.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_pass_validation_with_valid_range()
        {
            Subject.Min = 1;
            Subject.Max = 10;

            var result = Subject.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_have_correct_implementation_name()
        {
            Subject.ImplementationName.Should().Be("Size");
        }
    }
}
